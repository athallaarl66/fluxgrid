using System.Text;
using System.Text.Json;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class JobPostingService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _embedding;
    private readonly AuditService _audit;

    public JobPostingService(AppDbContext db, EmbeddingService embedding, AuditService audit)
    {
        _db = db;
        _embedding = embedding;
        _audit = audit;
    }

    public async Task<JobResponse> CreateAsync(CreateJobRequest request, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Requirements = request.Requirements,
            RequiredSkills = request.RequiredSkills ?? [],
            MinExperienceYears = request.MinExperienceYears,
            MaxExperienceYears = request.MaxExperienceYears,
            Location = request.Location,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Status = JobPostingStatus.Draft,
            TenantId = tenantId
        };

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CREATE", "job_postings", job.Id, ipAddress, userAgent,
            null, job);

        return ToResponse(job);
    }

    public async Task<JobResponse?> GetByIdAsync(Guid id, Guid tenantId)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId);
        return job is null ? null : ToResponse(job);
    }

    public async Task<PaginatedResponse<JobResponse>> GetListAsync(Guid tenantId,
        string? search, string? status, int page = 1, int pageSize = 20)
    {
        var query = _db.JobPostings.Where(j => j.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(term) ||
                j.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrEmpty(status) && JobPostingStatus.IsValid(status))
            query = query.Where(j => j.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobResponse(
                j.Id, j.Title, j.Description, j.Requirements,
                j.RequiredSkills, j.MinExperienceYears, j.MaxExperienceYears,
                j.Location, j.SalaryMin, j.SalaryMax,
                j.Status, j.TenantId, j.CreatedAt, j.UpdatedAt))
            .ToListAsync();

        return new PaginatedResponse<JobResponse>(items, total, page, pageSize);
    }

    public async Task<JobResponse?> UpdateAsync(Guid id, UpdateJobRequest request, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId);

        if (job is null) return null;

        var oldValues = new
        {
            job.Title, job.Description, job.Requirements, job.RequiredSkills,
            job.MinExperienceYears, job.MaxExperienceYears, job.Location,
            job.SalaryMin, job.SalaryMax
        };

        if (request.Title is not null) job.Title = request.Title;
        if (request.Description is not null) job.Description = request.Description;
        if (request.Requirements is not null) job.Requirements = request.Requirements;
        if (request.RequiredSkills is not null) job.RequiredSkills = request.RequiredSkills;
        if (request.MinExperienceYears.HasValue) job.MinExperienceYears = request.MinExperienceYears;
        if (request.MaxExperienceYears.HasValue) job.MaxExperienceYears = request.MaxExperienceYears;
        if (request.Location is not null) job.Location = request.Location;
        if (request.SalaryMin.HasValue) job.SalaryMin = request.SalaryMin;
        if (request.SalaryMax.HasValue) job.SalaryMax = request.SalaryMax;

        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "UPDATE", "job_postings", id, ipAddress, userAgent,
            oldValues, job);

        return ToResponse(job);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId);

        if (job is null) return false;

        if (job.Status != JobPostingStatus.Draft)
            throw new InvalidOperationException("Only DRAFT jobs can be deleted. Close or unpublish the job first.");

        _db.JobPostings.Remove(job);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "DELETE", "job_postings", id, ipAddress, userAgent,
            new { deletedJob = job }, null);

        return true;
    }

    public async Task<PublishJobResponse> PublishAsync(Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId)
            ?? throw new InvalidOperationException("Job posting not found");

        if (job.Status != JobPostingStatus.Draft)
            throw new InvalidOperationException($"Cannot publish job in status '{job.Status}'. Only DRAFT jobs can be published.");

        var text = EmbeddingService.ComposeJobText(job);
        var embedding = await _embedding.GenerateEmbeddingAsync(text);

        if (embedding is null)
        {
            return new PublishJobResponse(id, job.Status,
                "Failed to generate AI search index. Please try publishing again later.");
        }

        job.Embedding = embedding;
        job.Status = JobPostingStatus.Published;
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "PUBLISH", "job_postings", id, ipAddress, userAgent,
            new { previousStatus = JobPostingStatus.Draft },
            new { newStatus = JobPostingStatus.Published });

        return new PublishJobResponse(id, JobPostingStatus.Published, "Job published successfully");
    }

    public async Task<PublishJobResponse> CloseAsync(Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId)
            ?? throw new InvalidOperationException("Job posting not found");

        if (job.Status != JobPostingStatus.Published)
            throw new InvalidOperationException($"Cannot close job in status '{job.Status}'. Only PUBLISHED jobs can be closed.");

        job.Status = JobPostingStatus.Closed;
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CLOSE", "job_postings", id, ipAddress, userAgent,
            new { previousStatus = JobPostingStatus.Published },
            new { newStatus = JobPostingStatus.Closed });

        return new PublishJobResponse(id, JobPostingStatus.Closed, "Job closed successfully");
    }

    public async Task<JobMatchResponse> GetJobMatchesAsync(Guid jobId, Guid tenantId,
        double? minScore = null, int? limit = null, string? sortBy = null, string? matchType = null)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId)
            ?? throw new InvalidOperationException("Job posting not found");

        if (job.Status == JobPostingStatus.Draft)
            throw new InvalidOperationException("Job must be published to view matches");

        if (job.Status == JobPostingStatus.Closed)
            throw new InvalidOperationException("Job is closed");

        var now = DateTime.UtcNow;
        var items = new List<JobMatchItem>();

        if (string.IsNullOrEmpty(matchType) || matchType != "ai")
        {
            var manualMatches = await _db.CandidateJobMatches
                .Where(m => m.JobId == jobId && m.IsManual)
                .Join(_db.Candidates.Where(c => c.TenantId == tenantId),
                    m => m.CandidateId, c => c.Id, (m, c) => new { m, c })
                .Select(x => new JobMatchItem(
                    x.c.Id, x.c.Name, x.c.Email,
                    x.m.Score, null, null, null, null,
                    "MANUAL", x.m.CreatedAt))
                .ToListAsync();

            items.AddRange(manualMatches);
        }

        if (string.IsNullOrEmpty(matchType) || matchType != "manual")
        {
            if (job.Embedding is not null)
            {
                var candidates = await _db.Candidates
                    .Where(c => c.TenantId == tenantId
                        && c.Status == "ACTIVE"
                        && c.Embedding != null)
                    .Select(c => new { c.Id, c.Name, c.Email, c.Embedding })
                    .ToListAsync();

                var aiMatches = candidates
                    .Select(c => new
                    {
                        c.Id, c.Name, c.Email,
                        Score = CosineSimilarity(job.Embedding!, c.Embedding!)
                    })
                    .Where(x => !minScore.HasValue || x.Score >= minScore.Value)
                    .OrderByDescending(x => x.Score)
                    .Select(x => new JobMatchItem(
                        x.Id, x.Name, x.Email,
                        Math.Round(x.Score, 4), null, null, null, null,
                        "AI", now))
                    .ToList();

                items.AddRange(aiMatches);
            }
        }

        items = sortBy?.ToLower() switch
        {
            "name" => items.OrderBy(x => x.CandidateName).ToList(),
            "date" => items.OrderByDescending(x => x.CalculatedAt).ToList(),
            _ => items.OrderByDescending(x => x.MatchScore).ToList()
        };

        var take = Math.Clamp(limit ?? 50, 1, 100);
        items = items.Take(take).ToList();

        return new JobMatchResponse(jobId, job.Title, items);
    }

    public async Task<MatchReasoningResponse?> GetMatchReasoningAsync(
        Guid jobId, Guid candidateId, Guid tenantId, CancellationToken ct = default)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        var candidate = await _db.Candidates
            .Include(c => c.Education)
            .Include(c => c.Experience)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == candidateId && c.TenantId == tenantId);

        if (job is null || candidate is null) return null;

        var candidateProfile = EmbeddingService.ComposeCandidateText(candidate);
        var jobDescription = EmbeddingService.ComposeJobText(job);

        var reasoning = await _embedding.GenerateMatchReasoningAsync(candidateProfile, jobDescription, ct);

        double matchScore = 0;
        if (candidate.Embedding is not null && job.Embedding is not null)
            matchScore = Math.Round(CosineSimilarity(job.Embedding, candidate.Embedding), 4);

        return new MatchReasoningResponse(
            candidateId, candidate.Name, matchScore,
            reasoning ?? "Unable to generate reasoning at this time.");
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static JobResponse ToResponse(JobPosting job) => new(
        job.Id, job.Title, job.Description, job.Requirements,
        job.RequiredSkills, job.MinExperienceYears, job.MaxExperienceYears,
        job.Location, job.SalaryMin, job.SalaryMax,
        job.Status, job.TenantId, job.CreatedAt, job.UpdatedAt
    );
}
