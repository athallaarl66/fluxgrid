using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class RecruitmentServiceApprovalTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecruitmentService _service;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RecruitmentServiceApprovalTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _storageMock = new Mock<IFileStorageService>();
        _storageMock.Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new DomainEventDispatcher();
        var audit = new AuditService(_db);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Storage:BucketName"]).Returns("test-bucket");

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _service = new RecruitmentService(_db, _storageMock.Object, audit, dispatcher, scopeFactoryMock.Object, configMock.Object);
    }

    public void Dispose() => _db.Dispose();

    private Candidate CreateParsedCandidate()
    {
        var c = new Candidate
        {
            Id = Guid.NewGuid(),
            Name = "Test Candidate",
            Email = "test@test.com",
            Status = CandidateStatus.Parsed,
            TenantId = _tenantId,
            UploadedBy = _userId,
            FileHash = "hash123",
            OriginalFilename = "cv.pdf"
        };
        _db.Candidates.Add(c);
        _db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task ApproveCandidateAsync_SetsStatusToActive()
    {
        var c = CreateParsedCandidate();

        var result = await _service.ApproveCandidateAsync(c.Id, _tenantId, _userId);

        Assert.Equal(CandidateStatus.Active, result.Status);
        Assert.Equal(CandidateStatus.Active, (await _db.Candidates.FindAsync(c.Id))!.Status);
    }

    [Fact]
    public async Task ApproveCandidateAsync_Throws_WhenCandidateNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveCandidateAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task ApproveCandidateAsync_Throws_WhenStatusIsNotParsed()
    {
        var c = new Candidate
        {
            Id = Guid.NewGuid(), Name = "Draft", Email = "draft@test.com",
            Status = CandidateStatus.Draft, TenantId = _tenantId, UploadedBy = _userId
        };
        _db.Candidates.Add(c);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveCandidateAsync(c.Id, _tenantId, _userId));
        Assert.Contains("Only PARSED", ex.Message);
    }

    [Fact]
    public async Task ApproveCandidateAsync_RespectsTenantIsolation()
    {
        var c = CreateParsedCandidate();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveCandidateAsync(c.Id, Guid.NewGuid(), _userId));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task RejectCandidateAsync_SetsStatusToRejected()
    {
        var c = CreateParsedCandidate();

        var result = await _service.RejectCandidateAsync(c.Id, _tenantId, _userId);

        Assert.Equal(CandidateStatus.Rejected, result.Status);
        Assert.Equal(CandidateStatus.Rejected, (await _db.Candidates.FindAsync(c.Id))!.Status);
    }

    [Fact]
    public async Task RejectCandidateAsync_Throws_WhenCandidateNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectCandidateAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task RejectCandidateAsync_Throws_WhenStatusIsNotParsed()
    {
        var c = new Candidate
        {
            Id = Guid.NewGuid(), Name = "Active", Email = "active@test.com",
            Status = CandidateStatus.Active, TenantId = _tenantId, UploadedBy = _userId
        };
        _db.Candidates.Add(c);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectCandidateAsync(c.Id, _tenantId, _userId));
        Assert.Contains("Only PARSED", ex.Message);
    }

    [Fact]
    public async Task DeleteCandidateAsync_RemovesCandidateAndFile()
    {
        var c = CreateParsedCandidate();

        await _service.DeleteCandidateAsync(c.Id, _tenantId, _userId);

        Assert.Null(await _db.Candidates.FindAsync(c.Id));
        _storageMock.Verify(s => s.DeleteFileAsync("test-bucket",
            $"{_tenantId}/{c.FileHash}/{c.OriginalFilename}"), Times.Once);
    }

    [Fact]
    public async Task DeleteCandidateAsync_Throws_WhenCandidateNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteCandidateAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }
}
