using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class RecruitmentServiceEmbeddingTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecruitmentService _service;
    private readonly Mock<EmbeddingService> _embeddingMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RecruitmentServiceEmbeddingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        var audit = new AuditService(_db);
        var events = new DomainEventDispatcher();

        _embeddingMock = new Mock<EmbeddingService>(
            Mock.Of<IHttpClientFactory>(), Mock.Of<IConfiguration>());
        _embeddingMock.CallBase = true;

        _storageMock = new Mock<IFileStorageService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Storage:BucketName"]).Returns("test-bucket");

        _service = new RecruitmentService(
            _db, _storageMock.Object, audit, events,
            _embeddingMock.Object, _scopeFactoryMock.Object, configMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // ─── ApproveCandidateAsync ──────────────────────────────

    [Fact]
    public async Task ApproveCandidateAsync_GeneratesEmbeddingOnApprove()
    {
        var candidateId = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "alice@test.com",
            Status = CandidateStatus.Parsed, TenantId = _tenantId,
            Skills = [new CandidateSkill { SkillName = "C#" }]
        });
        await _db.SaveChangesAsync();

        var embedding = Enumerable.Range(0, 1536).Select(x => (float)x / 1536).ToArray();
        _embeddingMock
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var result = await _service.ApproveCandidateAsync(candidateId, _tenantId, _userId);

        Assert.Equal(CandidateStatus.Active, result.Status);
        Assert.Contains("successfully", result.Message);

        var saved = await _db.Candidates.FindAsync(candidateId);
        Assert.NotNull(saved.Embedding);
        Assert.Equal(1536, saved.Embedding.Length);
        Assert.Null(saved.EmbeddingStatus);
    }

    [Fact]
    public async Task ApproveCandidateAsync_QueuesRetryOnEmbeddingFailure()
    {
        var candidateId = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "alice@test.com",
            Status = CandidateStatus.Parsed, TenantId = _tenantId,
            Skills = [new CandidateSkill { SkillName = "C#" }]
        });
        await _db.SaveChangesAsync();

        var scopeMock = new Mock<IServiceScope>();
        var scopeServiceProvider = new Mock<IServiceProvider>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
        scopeServiceProvider.Setup(p => p.GetService(typeof(AppDbContext))).Returns(_db);
        scopeServiceProvider.Setup(p => p.GetService(typeof(EmbeddingService))).Returns(_embeddingMock.Object);
        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(scopeMock.Object);

        _embeddingMock
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((float[]?)null);

        var result = await _service.ApproveCandidateAsync(candidateId, _tenantId, _userId);

        Assert.Equal(CandidateStatus.Active, result.Status);
        Assert.Contains("queued for retry", result.Message);

        var saved = await _db.Candidates.FindAsync(candidateId);
        Assert.Null(saved.Embedding);
        Assert.Equal("PENDING", saved.EmbeddingStatus);
    }

    [Fact]
    public async Task ApproveCandidateAsync_ThrowsOnNonParsedCandidate()
    {
        var candidateId = Guid.NewGuid();
        _db.Candidates.Add(new Candidate
        {
            Id = candidateId, Name = "Alice", Email = "alice@test.com",
            Status = CandidateStatus.Draft, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveCandidateAsync(candidateId, _tenantId, _userId));
        Assert.Contains("Only PARSED", ex.Message);
    }
}
