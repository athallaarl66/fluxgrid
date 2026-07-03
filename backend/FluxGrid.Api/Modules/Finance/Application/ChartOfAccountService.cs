using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class ChartOfAccountService
{
    private readonly AppDbContext _db;

    public ChartOfAccountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccountTreeNode>> GetTreeAsync(Guid tenantId, bool flat = false)
    {
        var accounts = await _db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Code)
            .ToListAsync();

        if (flat)
        {
            return accounts.Select(a => MapToNode(a, 0, [])).ToList();
        }

        var lookup = accounts.ToLookup(a => a.ParentId);
        return BuildTree(lookup, null, 0);
    }

    public async Task<AccountResponse> CreateAsync(Guid tenantId, CreateAccountRequest request)
    {
        if (!AccountTypes.IsValid(request.Type))
            throw new InvalidOperationException($"Invalid account type. Must be one of: {string.Join(", ", AccountTypes.All)}");

        if (request.ParentId.HasValue)
        {
            var parent = await _db.ChartOfAccounts.FindAsync(request.ParentId.Value)
                ?? throw new InvalidOperationException("Parent account not found");

            var depth = await GetDepthAsync(parent.Id);
            if (depth >= 4)
                throw new InvalidOperationException("Maximum hierarchy depth (5 levels) exceeded");

            if (!parent.IsActive)
                throw new InvalidOperationException("Cannot create child under a deactivated parent");
        }

        var account = new ChartOfAccount
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            IsActive = request.IsActive,
            TenantId = tenantId
        };

        if (request.ParentId.HasValue)
        {
            var parent = await _db.ChartOfAccounts.FindAsync(request.ParentId.Value);
            account.ParentId = parent!.Id;
            account.Type = parent.Type;
        }
        else
        {
            account.Type = request.Type;
        }

        _db.ChartOfAccounts.Add(account);
        await _db.SaveChangesAsync();

        return MapToResponse(account);
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, Guid tenantId, UpdateAccountRequest request)
    {
        var account = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId)
            ?? throw new InvalidOperationException("Account not found");

        if (request.Code is not null) account.Code = request.Code;
        if (request.Name is not null) account.Name = request.Name;
        if (request.Type is not null)
        {
            if (!AccountTypes.IsValid(request.Type))
                throw new InvalidOperationException($"Invalid account type. Must be one of: {string.Join(", ", AccountTypes.All)}");

            account.Type = request.Type;
        }

        if (request.ParentId is not null && request.ParentId != account.ParentId)
        {
            if (request.ParentId == id)
                throw new InvalidOperationException("An account cannot be its own parent");

            if (await IsDescendantAsync(id, request.ParentId.Value))
                throw new InvalidOperationException("Circular reference detected: selected parent is a descendant of this account");
            account.ParentId = request.ParentId;
        }

        if (request.IsActive is not null)
        {
            if (!request.IsActive.Value)
            {
                await DeactivateCascadeAsync(account);
            }
            account.IsActive = request.IsActive.Value;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(account);
    }

    public async Task<AccountResponse> DeactivateAsync(Guid id, Guid tenantId)
    {
        var account = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId)
            ?? throw new InvalidOperationException("Account not found");

        if (!account.IsActive)
            throw new InvalidOperationException("Account is already deactivated");

        await DeactivateCascadeAsync(account);
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToResponse(account);
    }

    private async Task DeactivateCascadeAsync(ChartOfAccount account)
    {
        account.IsActive = false;
        var children = await _db.ChartOfAccounts
            .Where(a => a.ParentId == account.Id)
            .ToListAsync();
        foreach (var child in children)
        {
            await DeactivateCascadeAsync(child);
        }
    }

    private async Task<bool> IsDescendantAsync(Guid accountId, Guid candidateParentId)
    {
        var current = candidateParentId;
        while (true)
        {
            if (current == accountId) return true;
            var parent = await _db.ChartOfAccounts
                .Where(a => a.Id == current)
                .Select(a => a.ParentId)
                .FirstOrDefaultAsync();
            if (parent is null) return false;
            current = parent.Value;
        }
    }

    private async Task<int> GetDepthAsync(Guid accountId)
    {
        var depth = 0;
        var current = accountId;
        while (true)
        {
            var parentId = await _db.ChartOfAccounts
                .Where(a => a.Id == current)
                .Select(a => a.ParentId)
                .FirstOrDefaultAsync();
            if (parentId is null) return depth;
            current = parentId.Value;
            depth++;
        }
    }

    private static List<AccountTreeNode> BuildTree(ILookup<Guid?, ChartOfAccount> lookup, Guid? parentId, int depth)
    {
        return lookup[parentId]
            .OrderBy(a => a.Code)
            .Select(a => new AccountTreeNode(
                a.Id,
                a.Code,
                a.Name,
                a.ParentId,
                a.Type,
                a.IsActive,
                depth,
                BuildTree(lookup, a.Id, depth + 1)
            ))
            .ToList();
    }

    private static AccountTreeNode MapToNode(ChartOfAccount a, int depth, List<AccountTreeNode> children)
    {
        return new AccountTreeNode(a.Id, a.Code, a.Name, a.ParentId, a.Type, a.IsActive, depth, children);
    }

    private static AccountResponse MapToResponse(ChartOfAccount a)
    {
        return new AccountResponse(a.Id, a.Code, a.Name, a.ParentId, a.Type, a.IsActive, a.TenantId, a.CreatedAt, a.UpdatedAt);
    }
}
