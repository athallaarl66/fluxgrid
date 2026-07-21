using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class JobPostingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly JobPostingService _service;
    private readonly Mock<EmbeddingService> _embeddingMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public JobPostingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        var audit = new AuditService(_db);

        _embeddingMock = new Mock<EmbeddingService>(
            Mock.Of<IHttpClientFactory>(), Mock.Of<IConfiguration>());
        _embeddingMock.CallBase = true;

        _service = new JobPostingService(_db, _embeddingMock.Object, audit);
    }

    public void Dispose() => _db.Dispose();

    // ─── CreateAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesJobWithDraftStatus()
    {
        var request = new CreateJobRequest("Engineer", "Build stuff", null, null, null, null, null, null, null);

        var result = await _service.CreateAsync(request, _tenantId, _userId);

        Assert.NotNull(result);
        Assert.Equal("Engineer", result.Title);
        Assert.Equal("Build stuff", result.Description);
        Assert.Equal(JobPostingStatus.Draft, result.Status);
        Assert.Equal(_tenantId, result.TenantId);
    }

    [Fact]
    public async Task CreateAsync_SavesToDatabase()
    {
        var request = new CreateJobRequest("Engineer", "Build stuff", null, null, null, null, null, null, null);

        var result = await _service.CreateAsync(request, _tenantId, _userId);

        var saved = await _db.JobPostings.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Engineer", saved.Title);
    }

    [Fact]
    public async Task CreateAsync_RespectsTenant()
    {
        var otherTenant = Guid.NewGuid();
        var request = new CreateJobRequest("Engineer", "Build stuff", null, null, null, null, null, null, null);
        await _service.CreateAsync(request, otherTenant, _userId);

        var all = await _db.JobPostings.Where(j => j.TenantId == _tenantId).ToListAsync();
        Assert.Empty(all);
    }

    // ─── GetByIdAsync ───────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsJob()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Test", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _tenantId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_RespectsTenantIsolation()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Test", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId);
        Assert.Null(result);
    }

    // ─── GetListAsync ───────────────────────────────────────

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 5; i++)
            _db.JobPostings.Add(new JobPosting
            {
                Id = Guid.NewGuid(), Title = $"Job {i}", Description = "Desc",
                Status = JobPostingStatus.Draft, TenantId = _tenantId
            });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetListAsync_FiltersBySearch()
    {
        _db.JobPostings.Add(new JobPosting
        {
            Id = Guid.NewGuid(), Title = "Unique Title", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, "Unique", null, 1, 20);

        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStatus()
    {
        _db.JobPostings.Add(new JobPosting
        {
            Id = Guid.NewGuid(), Title = "Published Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        _db.JobPostings.Add(new JobPosting
        {
            Id = Guid.NewGuid(), Title = "Draft Job", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, JobPostingStatus.Published, 1, 20);

        Assert.Equal(1, result.Total);
        Assert.Equal("Published Job", result.Items[0].Title);
    }

    [Fact]
    public async Task GetListAsync_RespectsTenantIsolation()
    {
        _db.JobPostings.Add(new JobPosting
        {
            Id = Guid.NewGuid(), Title = "Other Tenant", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, 1, 20);

        Assert.Equal(0, result.Total);
    }

    // ─── UpdateAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Old", Description = "Old desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new UpdateJobRequest("New Title", null, null, null, null, null, null, null, null);
        var result = await _service.UpdateAsync(id, request, _tenantId, _userId);

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("Old desc", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullWhenNotFound()
    {
        var request = new UpdateJobRequest("New", null, null, null, null, null, null, null, null);
        var result = await _service.UpdateAsync(Guid.NewGuid(), request, _tenantId, _userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_RespectsTenantIsolation()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Old", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var request = new UpdateJobRequest("New", null, null, null, null, null, null, null, null);
        var result = await _service.UpdateAsync(id, request, _tenantId, _userId);
        Assert.Null(result);
    }

    // ─── DeleteAsync ────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_DeletesDraftJob()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Delete Me", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(id, _tenantId, _userId);

        Assert.True(result);
        Assert.Null(await _db.JobPostings.FindAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsOnNonDraft()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Published", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(id, _tenantId, _userId));
        Assert.Contains("Only DRAFT", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenNotFound()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid(), _tenantId, _userId);
        Assert.False(result);
    }

    // ─── PublishAsync ───────────────────────────────────────

    [Fact]
    public async Task PublishAsync_GeneratesEmbeddingAndPublishes()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Job", Description = "Desc",
            RequiredSkills = ["C#"],
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var embedding = Enumerable.Range(0, 1536).Select(x => (float)x / 1536).ToArray();
        _embeddingMock
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var result = await _service.PublishAsync(id, _tenantId, _userId);

        Assert.Equal(JobPostingStatus.Published, result.Status);
        Assert.Contains("successfully", result.Message);

        var saved = await _db.JobPostings.FindAsync(id);
        Assert.NotNull(saved.Embedding);
        Assert.Equal(1536, saved.Embedding.Length);
    }

    [Fact]
    public async Task PublishAsync_ThrowsOnNonDraft()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Published", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PublishAsync(id, _tenantId, _userId));
        Assert.Contains("Only DRAFT", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_ThrowsOnNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PublishAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_HandlesEmbeddingFailureGracefully()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        _embeddingMock
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((float[]?)null);

        var result = await _service.PublishAsync(id, _tenantId, _userId);

        Assert.Equal(JobPostingStatus.Draft, result.Status);
        Assert.Contains("Failed to generate", result.Message);
    }

    // ─── CloseAsync ─────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_SetsStatusToClosed()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.CloseAsync(id, _tenantId, _userId);

        Assert.Equal(JobPostingStatus.Closed, result.Status);
        Assert.Contains("successfully", result.Message);

        var saved = await _db.JobPostings.FindAsync(id);
        Assert.Equal(JobPostingStatus.Closed, saved.Status);
    }

    [Fact]
    public async Task CloseAsync_ThrowsOnNonPublished()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Draft", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloseAsync(id, _tenantId, _userId));
        Assert.Contains("Only PUBLISHED", ex.Message);
    }

    // ─── GetJobMatchesAsync ──────────────────────────────────

    [Fact]
    public async Task GetJobMatchesAsync_ReturnsRankedMatches()
    {
        var jobId = Guid.NewGuid();
        var jobEmbedding = Enumerable.Range(0, 1536).Select(x => (float)1).ToArray();
        _db.JobPostings.Add(new JobPosting
        {
            Id = jobId, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId,
            Embedding = jobEmbedding
        });

        var candidateId = Guid.NewGuid();
        var candEmbedding = Enumerable.Range(0, 1536).Select(x => (float)0.9).ToArray();
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "alice@test.com",
            Status = "ACTIVE", TenantId = _tenantId, Embedding = candEmbedding
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetJobMatchesAsync(jobId, _tenantId);

        Assert.Equal(jobId, result.JobId);
        Assert.Single(result.Matches);
        Assert.Equal(candidateId, result.Matches[0].CandidateId);
        Assert.True(result.Matches[0].MatchScore > 0);
    }

    [Fact]
    public async Task GetJobMatchesAsync_ThrowsOnDraftJob()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Draft", Description = "Desc",
            Status = JobPostingStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetJobMatchesAsync(id, _tenantId));
        Assert.Contains("published", ex.Message);
    }

    [Fact]
    public async Task GetJobMatchesAsync_ThrowsOnClosedJob()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Closed", Description = "Desc",
            Status = JobPostingStatus.Closed, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetJobMatchesAsync(id, _tenantId));
        Assert.Contains("closed", ex.Message);
    }

    [Fact]
    public async Task GetJobMatchesAsync_ReturnsEmptyWhenNoEmbedding()
    {
        var id = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = id, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetJobMatchesAsync(id, _tenantId);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task GetJobMatchesAsync_RespectsMinScore()
    {
        var jobId = Guid.NewGuid();
        var jobEmbedding = Enumerable.Range(0, 1536).Select(x => (float)1).ToArray();
        _db.JobPostings.Add(new JobPosting
        {
            Id = jobId, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId,
            Embedding = jobEmbedding
        });

        var candEmbedding = new float[1536];
        candEmbedding[0] = 1;
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com",
            Status = "ACTIVE", TenantId = _tenantId,
            Embedding = candEmbedding
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetJobMatchesAsync(jobId, _tenantId, minScore: 0.99);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task GetJobMatchesAsync_RespectsLimit()
    {
        var jobId = Guid.NewGuid();
        var jobEmbedding = Enumerable.Range(0, 1536).Select(x => (float)1).ToArray();
        _db.JobPostings.Add(new JobPosting
        {
            Id = jobId, Title = "Job", Description = "Desc",
            Status = JobPostingStatus.Published, TenantId = _tenantId,
            Embedding = jobEmbedding
        });

        for (int i = 0; i < 5; i++)
        {
            _db.Candidates.Add(new Candidate
            {
                Id = Guid.NewGuid(), Name = $"Cand {i}", Email = $"c{i}@test.com",
                Status = "ACTIVE", TenantId = _tenantId,
                Embedding = Enumerable.Range(0, 1536).Select(x => (float)0.9).ToArray()
            });
        }
        await _db.SaveChangesAsync();

        var result = await _service.GetJobMatchesAsync(jobId, _tenantId, limit: 2);
        Assert.Equal(2, result.Matches.Count);
    }

    // ─── GetMatchReasoningAsync ─────────────────────────────

    [Fact]
    public async Task GetMatchReasoningAsync_ReturnsReasoning()
    {
        var jobId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var jobEmbedding = Enumerable.Range(0, 1536).Select(x => (float)1).ToArray();
        var candEmbedding = Enumerable.Range(0, 1536).Select(x => (float)0.9).ToArray();

        _db.JobPostings.Add(new JobPosting
        {
            Id = jobId, Title = "Engineer", Description = "Build",
            Status = JobPostingStatus.Published, TenantId = _tenantId,
            Embedding = jobEmbedding
        });
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "a@test.com",
            Status = "ACTIVE", TenantId = _tenantId,
            Embedding = candEmbedding,
            Skills = [new CandidateSkill { SkillName = "C#" }]
        });
        await _db.SaveChangesAsync();

        _embeddingMock
            .Setup(e => e.GenerateMatchReasoningAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Good match based on skills.");

        var result = await _service.GetMatchReasoningAsync(jobId, candidateId, _tenantId);

        Assert.NotNull(result);
        Assert.Equal(candidateId, result.CandidateId);
        Assert.Equal("Alice", result.CandidateName);
        Assert.True(result.MatchScore > 0);
        Assert.Equal("Good match based on skills.", result.Reasoning);
    }

    [Fact]
    public async Task GetMatchReasoningAsync_ReturnsNullWhenJobNotFound()
    {
        var candidateId = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "a@test.com",
            Status = "ACTIVE", TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetMatchReasoningAsync(Guid.NewGuid(), candidateId, _tenantId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMatchReasoningAsync_ReturnsNullWhenCandidateNotFound()
    {
        var jobId = Guid.NewGuid();
        _db.JobPostings.Add(new JobPosting
        {
            Id = jobId, Title = "Engineer", Description = "Build",
            Status = JobPostingStatus.Published, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetMatchReasoningAsync(jobId, Guid.NewGuid(), _tenantId);
        Assert.Null(result);
    }
}
