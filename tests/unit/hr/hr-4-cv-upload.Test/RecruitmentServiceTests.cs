using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class RecruitmentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecruitmentService _service;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RecruitmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _storageMock = new Mock<IFileStorageService>();
        _dispatcher = new DomainEventDispatcher();
        var audit = new AuditService(_db);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Storage:BucketName"]).Returns("test-bucket");

        _service = new RecruitmentService(_db, _storageMock.Object, audit, _dispatcher, _configMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── RequestUploadUrlAsync ─────────────────────────────────

    [Fact]
    public async Task RequestUploadUrlAsync_ReturnsPresignedUrl()
    {
        var request = new UploadUrlRequest("cv.pdf", "pdf", 100_000, "abc123");
        _storageMock.Setup(s => s.GeneratePresignedUploadUrlAsync("test-bucket", It.IsAny<string>(), "pdf", 5))
            .ReturnsAsync("https://presigned.url/upload");

        var result = await _service.RequestUploadUrlAsync(request, _tenantId, _userId);

        Assert.NotNull(result);
        Assert.Equal("https://presigned.url/upload", result.PresignedUrl);
        Assert.Equal("abc123", result.FileHash);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_ThrowsOnInvalidFileType()
    {
        var request = new UploadUrlRequest("malware.exe", "exe", 100_000, "abc123");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RequestUploadUrlAsync(request, _tenantId, _userId));
        Assert.Contains("Only PDF and DOCX", ex.Message);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_ThrowsOnOversizedFile()
    {
        var request = new UploadUrlRequest("big.pdf", "pdf", 6 * 1024 * 1024, "abc123");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RequestUploadUrlAsync(request, _tenantId, _userId));
        Assert.Contains("exceeds maximum size", ex.Message);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_ThrowsOnDuplicateHash()
    {
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Existing", Email = "ex@test.com",
            FileHash = "dup_hash", TenantId = _tenantId, Status = "DRAFT",
            UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var request = new UploadUrlRequest("dup.pdf", "pdf", 100_000, "dup_hash");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RequestUploadUrlAsync(request, _tenantId, _userId));
        Assert.Contains("already been uploaded", ex.Message);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_AllowsSameHashForDifferentTenant()
    {
        var otherTenant = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Existing", Email = "ex@test.com",
            FileHash = "shared_hash", TenantId = otherTenant, Status = "DRAFT",
            UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var request = new UploadUrlRequest("cv.pdf", "pdf", 100_000, "shared_hash");
        _storageMock.Setup(s => s.GeneratePresignedUploadUrlAsync("test-bucket", It.IsAny<string>(), "pdf", 5))
            .ReturnsAsync("https://presigned.url/upload");

        var result = await _service.RequestUploadUrlAsync(request, _tenantId, _userId);

        Assert.NotNull(result);
    }

    // ─── CreateCandidateAsync ──────────────────────────────────

    [Fact]
    public async Task CreateCandidateAsync_CreatesDraftCandidate()
    {
        var request = new CreateCandidateRequest(
            "John Doe", "john@test.com", null, null, null, null, null,
            null, null, null, null, null,
            "obj-key", "file-hash-123", "cv.pdf", "pdf", 100_000);

        var result = await _service.CreateCandidateAsync(request, _tenantId, _userId);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(1, await _db.Candidates.CountAsync());
    }

    [Fact]
    public async Task CreateCandidateAsync_ThrowsOnDuplicateEmail()
    {
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Existing", Email = "dup@test.com",
            Status = "DRAFT", TenantId = _tenantId, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var request = new CreateCandidateRequest(
            "John Doe", "dup@test.com", null, null, null, null, null,
            null, null, null, null, null,
            "obj-key", "hash", "cv.pdf", "pdf", 100_000);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCandidateAsync(request, _tenantId, _userId));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateCandidateAsync_RaisesCandidateUploadedEvent()
    {
        var request = new CreateCandidateRequest(
            "John Doe", "john.event@test.com", null, null, null, null, null,
            null, null, null, null, null,
            "obj-key", "hash-event", "cv.pdf", "pdf", 100_000);

        await _service.CreateCandidateAsync(request, _tenantId, _userId);

        Assert.Single(_dispatcher.GetEvents());
        Assert.IsType<CandidateUploaded>(_dispatcher.GetEvents()[0]);
        var ev = (CandidateUploaded)_dispatcher.GetEvents()[0];
        Assert.Equal("John Doe", ev.CandidateName);
        Assert.Equal("john.event@test.com", ev.Email);
    }

    [Fact]
    public async Task CreateCandidateAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Other", Email = "other@test.com",
            Status = "DRAFT", TenantId = otherTenant, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var request = new CreateCandidateRequest(
            "John Doe", "john@test.com", null, null, null, null, null,
            null, null, null, null, null,
            "obj-key", "hash", "cv.pdf", "pdf", 100_000);

        await _service.CreateCandidateAsync(request, _tenantId, _userId);

        Assert.Equal(2, await _db.Candidates.CountAsync());
    }

    // ─── GetCandidatesAsync ────────────────────────────────────

    [Fact]
    public async Task GetCandidatesAsync_ReturnsPaginatedResults()
    {
        SeedCandidates(5);
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(_tenantId, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetCandidatesAsync_FiltersBySearch()
    {
        SeedCandidates(3);
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Zara Unique", Email = "zara@test.com",
            Status = "DRAFT", TenantId = _tenantId, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(_tenantId, "zara", null, 1, 20);

        Assert.Equal(1, result.Total);
        Assert.Contains("Zara", result.Items[0].Name);
    }

    [Fact]
    public async Task GetCandidatesAsync_FiltersByStatus()
    {
        SeedCandidates(3);
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Active One", Email = "active@test.com",
            Status = "ACTIVE", TenantId = _tenantId, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(_tenantId, null, "ACTIVE", 1, 20);

        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetCandidatesAsync_OrdersByCreatedAtDesc()
    {
        var old = new Candidate
        {
            Id = Guid.NewGuid(), Name = "Old", Email = "old@test.com",
            Status = "DRAFT", CreatedAt = DateTime.UtcNow.AddDays(-2),
            TenantId = _tenantId, UploadedBy = _userId
        };
        var mid = new Candidate
        {
            Id = Guid.NewGuid(), Name = "Mid", Email = "mid@test.com",
            Status = "DRAFT", CreatedAt = DateTime.UtcNow.AddDays(-1),
            TenantId = _tenantId, UploadedBy = _userId
        };
        var recent = new Candidate
        {
            Id = Guid.NewGuid(), Name = "Recent", Email = "recent@test.com",
            Status = "DRAFT", CreatedAt = DateTime.UtcNow,
            TenantId = _tenantId, UploadedBy = _userId
        };
        _db.Candidates.AddRange(old, mid, recent);
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(_tenantId, null, null, 1, 20);

        Assert.Equal("Recent", result.Items[0].Name);
        Assert.Equal("Mid", result.Items[1].Name);
        Assert.Equal("Old", result.Items[2].Name);
    }

    [Fact]
    public async Task GetCandidatesAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        SeedCandidates(3);
        _db.Candidates.Add(new Candidate
        {
            Id = Guid.NewGuid(), Name = "Other", Email = "other@test.com",
            Status = "DRAFT", TenantId = otherTenant, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(_tenantId, null, null, 1, 20);

        Assert.Equal(3, result.Total);
    }

    // ─── GetCandidateDetailAsync ───────────────────────────────

    [Fact]
    public async Task GetCandidateDetailAsync_ReturnsCandidateWithSubEntities()
    {
        var id = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = id, Name = "Detail", Email = "detail@test.com",
            Status = "DRAFT", TenantId = _tenantId, UploadedBy = _userId,
            Education = [new CandidateEducation { Institution = "MIT", Degree = "BSc", CandidateId = id }],
            Skills = [new CandidateSkill { SkillName = "C#", CandidateId = id }]
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidateDetailAsync(id, _tenantId);

        Assert.NotNull(result);
        Assert.Equal("Detail", result.Name);
        Assert.Single(result.Education);
        Assert.Equal("MIT", result.Education[0].Institution);
        Assert.Single(result.Skills);
        Assert.Equal("C#", result.Skills[0].SkillName);
    }

    [Fact]
    public async Task GetCandidateDetailAsync_ReturnsNullWhenNotFound()
    {
        var result = await _service.GetCandidateDetailAsync(Guid.NewGuid(), _tenantId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCandidateDetailAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        var id = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = id, Name = "Other", Email = "other@test.com",
            Status = "DRAFT", TenantId = otherTenant, UploadedBy = _userId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetCandidateDetailAsync(id, _tenantId);

        Assert.Null(result);
    }

    // ─── Helpers ──────────────────────────────────────────────

    private void SeedCandidates(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            _db.Candidates.Add(new Candidate
            {
                Id = Guid.NewGuid(),
                Name = $"Candidate {i}",
                Email = $"candidate{i}@test.com",
                Status = "DRAFT",
                TenantId = _tenantId,
                UploadedBy = _userId
            });
        }
    }
}
