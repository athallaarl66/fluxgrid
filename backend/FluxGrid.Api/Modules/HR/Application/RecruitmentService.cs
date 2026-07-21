using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class RecruitmentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _events;
    private readonly EmbeddingService _embedding;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _bucketName;

    private static readonly string[] AllowedFileTypes = ["pdf", "docx"];
    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];
    private const long MaxFileSize = 5 * 1024 * 1024;

    public RecruitmentService(
        AppDbContext db,
        IFileStorageService storage,
        AuditService audit,
        DomainEventDispatcher events,
        EmbeddingService embedding,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _events = events;
        _embedding = embedding;
        _scopeFactory = scopeFactory;
        _bucketName = config["Storage:BucketName"] ?? "fluxgrid-cvs";
    }

    public async Task<UploadUrlResponse> RequestUploadUrlAsync(
        UploadUrlRequest request, Guid tenantId, Guid userId)
    {
        if (!AllowedFileTypes.Contains(request.FileType.ToLower()))
            throw new InvalidOperationException("Only PDF and DOCX files are allowed");

        if (request.FileSize > MaxFileSize)
            throw new InvalidOperationException("File exceeds maximum size of 5MB");

        var existing = await _db.Candidates
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.FileHash == request.FileHash);

        if (existing is not null)
            throw new InvalidOperationException(
                $"This file has already been uploaded. The existing candidate record is: {existing.Name}");

        var objectKey = $"{tenantId}/{request.FileHash}/{request.FileName}";
        var presignedUrl = await _storage.GeneratePresignedUploadUrlAsync(
            _bucketName, objectKey, request.FileType, 5);

        return new UploadUrlResponse(presignedUrl, objectKey, request.FileHash);
    }

    public async Task<CandidateResponse> CreateCandidateAsync(
        CreateCandidateRequest request, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        if (await _db.Candidates.AnyAsync(c => c.TenantId == tenantId && c.Email == request.Email))
            throw new InvalidOperationException("A candidate with this email already exists");

        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Location = request.Location,
            LinkedInUrl = request.LinkedInUrl,
            GitHubUrl = request.GitHubUrl,
            PortfolioUrl = request.PortfolioUrl,
            Summary = request.Summary,
            TotalExperienceMonths = request.TotalExperienceMonths,
            ExpectedSalaryMin = request.ExpectedSalaryMin,
            ExpectedSalaryMax = request.ExpectedSalaryMax,
            NoticePeriodDays = request.NoticePeriodDays,
            Status = CandidateStatus.Draft,
            FileUrl = request.FileUrl,
            FileHash = request.FileHash,
            OriginalFilename = request.OriginalFilename,
            FileType = request.FileType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedBy = userId,
            TenantId = tenantId
        };

        try
        {
            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var objectKey = $"{tenantId}/{request.FileHash}/{request.OriginalFilename}";
            await _storage.DeleteFileAsync(_bucketName, objectKey);

            if (await _db.Candidates.AnyAsync(c => c.TenantId == tenantId && c.FileHash == request.FileHash))
                throw new InvalidOperationException("A candidate with this file hash already exists");

            throw;
        }

        await _audit.LogAsync(userId, tenantId, "CREATE", "candidates", candidate.Id, ipAddress, userAgent, null, candidate);

        _events.Raise(new CandidateUploaded(
            candidate.Id, candidate.Name, candidate.Email,
            request.OriginalFilename, request.FileHash, request.FileSizeBytes,
            userId, tenantId));

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var cvParsing = scope.ServiceProvider.GetRequiredService<CvParsingService>();
            try { await cvParsing.ParseCandidateAsync(candidate.Id, userId, tenantId, ipAddress, userAgent); }
            catch { /* parsing failure handled inside service */ }
        });

        return new CandidateResponse(
            candidate.Id, candidate.Name, candidate.Email, candidate.Status,
            candidate.OriginalFilename, candidate.FileType, candidate.CreatedAt, candidate.TenantId);
    }

    public async Task<PaginatedResponse<CandidateListItem>> GetCandidatesAsync(
        Guid tenantId, string? search, string? status, int page = 1, int pageSize = 20)
    {
        var query = _db.Candidates.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrEmpty(status) && CandidateStatus.IsValid(status))
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CandidateListItem(
                c.Id, c.Name, c.Email, c.Status,
                c.OriginalFilename, c.FileType, c.CreatedAt))
            .ToListAsync();

        return new PaginatedResponse<CandidateListItem>(items, total, page, pageSize);
    }

    public async Task<ApproveCandidateResponse> ApproveCandidateAsync(Guid id, Guid tenantId,
        Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var candidate = await _db.Candidates
            .Include(c => c.Education)
            .Include(c => c.Experience)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
            ?? throw new InvalidOperationException("Candidate not found");

        if (candidate.Status != CandidateStatus.Parsed)
            throw new InvalidOperationException($"Cannot approve candidate in status '{candidate.Status}'. Only PARSED candidates can be approved.");

        candidate.Status = CandidateStatus.Active;
        candidate.UpdatedAt = DateTime.UtcNow;

        var text = EmbeddingService.ComposeCandidateText(candidate);
        var embedding = await _embedding.GenerateEmbeddingAsync(text);

        if (embedding is not null)
        {
            candidate.Embedding = embedding;
        }
        else
        {
            candidate.EmbeddingStatus = "PENDING";
            QueueRetryEmbedding(candidate.Id, userId, tenantId, ipAddress, userAgent);
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "APPROVE", "candidates", id, ipAddress, userAgent,
            new { previousStatus = CandidateStatus.Parsed },
            new { newStatus = CandidateStatus.Active, embeddingGenerated = embedding is not null });

        var msg = embedding is not null
            ? "Candidate approved successfully"
            : "Candidate approved but embedding generation queued for retry";

        return new ApproveCandidateResponse(id, CandidateStatus.Active, msg);
    }

    public async Task<RejectCandidateResponse> RejectCandidateAsync(Guid id, Guid tenantId,
        Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var candidate = await _db.Candidates
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
            ?? throw new InvalidOperationException("Candidate not found");

        if (candidate.Status != CandidateStatus.Parsed)
            throw new InvalidOperationException($"Cannot reject candidate in status '{candidate.Status}'. Only PARSED candidates can be rejected.");

        candidate.Status = CandidateStatus.Rejected;
        candidate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "REJECT", "candidates", id, ipAddress, userAgent,
            new { previousStatus = CandidateStatus.Parsed },
            new { newStatus = CandidateStatus.Rejected });

        return new RejectCandidateResponse(id, CandidateStatus.Rejected, "Candidate rejected");
    }

    public async Task DeleteCandidateAsync(Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var candidate = await _db.Candidates
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
            ?? throw new InvalidOperationException("Candidate not found");

        var objectKey = $"{tenantId}/{candidate.FileHash}/{candidate.OriginalFilename}";
        await _storage.DeleteFileAsync(_bucketName, objectKey);

        _db.Candidates.Remove(candidate);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "DELETE", "candidates", id, ipAddress, userAgent,
            new { deletedCandidate = candidate },
            null);
    }

    private void QueueRetryEmbedding(Guid candidateId, Guid userId, Guid tenantId,
        string? ipAddress, string? userAgent)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var embedding = scope.ServiceProvider.GetRequiredService<EmbeddingService>();

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(5, attempt)));

                var candidate = await db.Candidates
                    .Include(c => c.Education)
                    .Include(c => c.Experience)
                    .Include(c => c.Skills)
                    .FirstOrDefaultAsync(c => c.Id == candidateId);

                if (candidate is null || candidate.Embedding is not null) return;

                var text = EmbeddingService.ComposeCandidateText(candidate);
                var vector = await embedding.GenerateEmbeddingAsync(text);

                if (vector is not null)
                {
                    candidate.Embedding = vector;
                    candidate.EmbeddingStatus = null;
                    candidate.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return;
                }
            }
        });
    }

    public async Task<CandidateDetailResponse?> GetCandidateDetailAsync(Guid id, Guid tenantId)
    {
        return await _db.Candidates
            .Where(c => c.Id == id && c.TenantId == tenantId)
            .Select(c => new CandidateDetailResponse(
                c.Id, c.Name, c.Email, c.Phone, c.Location,
                c.LinkedInUrl, c.GitHubUrl, c.PortfolioUrl, c.Summary,
                c.TotalExperienceMonths, c.ExpectedSalaryMin, c.ExpectedSalaryMax,
                c.NoticePeriodDays, c.Status, c.FileUrl, c.OriginalFilename,
                c.FileType, c.FileSizeBytes, c.CreatedAt, c.UpdatedAt,
                c.Education.Select(e => new CandidateEducationResponse(
                    e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Gpa)).ToList(),
                c.Experience.Select(e => new CandidateExperienceResponse(
                    e.Id, e.Company, e.Role, e.StartDate, e.EndDate, e.IsCurrent, e.Description, e.Location)).ToList(),
                c.Skills.Select(s => new CandidateSkillResponse(
                    s.Id, s.SkillName, s.SkillCategory, s.ProficiencyLevel, s.YearsExperience)).ToList(),
                c.Documents.Select(d => new CandidateDocumentResponse(
                    d.Id, d.FileName, d.FileType, d.FileUrl, d.FileSizeBytes, d.IsPrimary, d.UploadedAt)).ToList()))
            .FirstOrDefaultAsync();
    }
}
