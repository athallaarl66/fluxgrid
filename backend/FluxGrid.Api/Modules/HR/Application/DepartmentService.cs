using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class DepartmentService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _events;

    public DepartmentService(AppDbContext db, AuditService audit, DomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _events = events;
    }

    public async Task<List<DepartmentResponse>> GetAllAsync(Guid tenantId)
    {
        return await _db.Departments
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.ParentId, d.IsActive, d.TenantId))
            .ToListAsync();
    }

    public async Task<DepartmentResponse> CreateAsync(
        Guid tenantId, CreateDepartmentRequest request, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        if (await _db.Departments.AnyAsync(d => d.TenantId == tenantId && d.Name == request.Name))
            throw new InvalidOperationException("A department with this name already exists");

        if (request.ParentId.HasValue)
        {
            var parent = await _db.Departments.FindAsync(request.ParentId.Value)
                ?? throw new InvalidOperationException("Parent department not found");

            var depth = await GetDepthAsync(parent.Id);
            if (depth >= 4)
                throw new InvalidOperationException("Maximum hierarchy depth (5 levels) exceeded");
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ParentId = request.ParentId,
            TenantId = tenantId
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CREATE", "departments", department.Id, ipAddress, userAgent, null, department);

        return new DepartmentResponse(department.Id, department.Name, department.ParentId, department.IsActive, department.TenantId);
    }

    public async Task<DepartmentResponse> UpdateAsync(
        Guid id, Guid tenantId, UpdateDepartmentRequest request, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId)
            ?? throw new InvalidOperationException("Department not found");

        if (request.Name is not null && request.Name != department.Name)
        {
            if (await _db.Departments.AnyAsync(d => d.TenantId == tenantId && d.Name == request.Name && d.Id != id))
                throw new InvalidOperationException("A department with this name already exists");
            department.Name = request.Name;
        }

        if (request.ParentId is not null && request.ParentId != department.ParentId)
        {
            if (request.ParentId == id)
                throw new InvalidOperationException("A department cannot be its own parent");

            if (await IsDescendantAsync(id, request.ParentId.Value))
                throw new InvalidOperationException("Circular reference detected");

            var depth = await GetDepthAsync(request.ParentId.Value);
            if (depth >= 4)
                throw new InvalidOperationException("Maximum hierarchy depth (5 levels) exceeded");

            department.ParentId = request.ParentId;
        }

        if (request.IsActive is not null)
            department.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "UPDATE", "departments", department.Id, ipAddress, userAgent, null, department);

        return new DepartmentResponse(department.Id, department.Name, department.ParentId, department.IsActive, department.TenantId);
    }

    public async Task DeleteAsync(
        Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId)
            ?? throw new InvalidOperationException("Department not found");

        if (await _db.Employees.AnyAsync(e => e.DepartmentId == id))
            throw new InvalidOperationException("Cannot delete department: employees are assigned to it");

        if (await _db.Departments.AnyAsync(d => d.ParentId == id))
            throw new InvalidOperationException("Cannot delete department: it has child departments");

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "DELETE", "departments", department.Id, ipAddress, userAgent, department, null);
    }

    private async Task<bool> IsDescendantAsync(Guid departmentId, Guid candidateParentId)
    {
        var current = candidateParentId;
        while (true)
        {
            if (current == departmentId) return true;
            var parentId = await _db.Departments
                .Where(d => d.Id == current)
                .Select(d => d.ParentId)
                .FirstOrDefaultAsync();
            if (parentId is null) return false;
            current = parentId.Value;
        }
    }

    private async Task<int> GetDepthAsync(Guid departmentId)
    {
        var depth = 0;
        var current = departmentId;
        while (true)
        {
            var parentId = await _db.Departments
                .Where(d => d.Id == current)
                .Select(d => d.ParentId)
                .FirstOrDefaultAsync();
            if (parentId is null) return depth;
            current = parentId.Value;
            depth++;
        }
    }
}
