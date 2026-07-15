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

    private static JobResponse ToResponse(JobPosting job) => new(
        job.Id, job.Title, job.Description, job.Requirements,
        job.RequiredSkills, job.MinExperienceYears, job.MaxExperienceYears,
        job.Location, job.SalaryMin, job.SalaryMax,
        job.Status, job.TenantId, job.CreatedAt, job.UpdatedAt
    );
}
