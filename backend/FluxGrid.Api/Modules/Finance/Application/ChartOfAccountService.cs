using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class ChartOfAccountService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public ChartOfAccountService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
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

    public async Task<AccountResponse> CreateAsync(Guid tenantId, CreateAccountRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        if (!AccountTypes.IsValid(request.Type))
            throw new InvalidOperationException($"Invalid account type. Must be one of: {string.Join(", ", AccountTypes.All)}");

        if (await _db.ChartOfAccounts.AnyAsync(a => a.TenantId == tenantId && a.Code == request.Code))
            throw new InvalidOperationException("Account code already exists for this tenant");

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

        await _audit.LogAsync(userId, tenantId, "CREATE", "chart_of_accounts", account.Id, ipAddress, userAgent, null, MapToResponse(account));

        return MapToResponse(account);
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, Guid tenantId, UpdateAccountRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var account = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId)
            ?? throw new InvalidOperationException("Account not found");

        var before = MapToResponse(account);

        if (request.Code is not null && request.Code != account.Code)
        {
            if (await _db.ChartOfAccounts.AnyAsync(a => a.TenantId == tenantId && a.Code == request.Code && a.Id != id))
                throw new InvalidOperationException("Account code already exists for this tenant");
            account.Code = request.Code;
        }
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

        var after = MapToResponse(account);
        await _audit.LogAsync(userId, tenantId, "UPDATE", "chart_of_accounts", account.Id, ipAddress, userAgent, before, after);

        return after;
    }

    public async Task<AccountResponse> DeactivateAsync(Guid id, Guid tenantId, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var account = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId)
            ?? throw new InvalidOperationException("Account not found");

        if (!account.IsActive)
            throw new InvalidOperationException("Account is already deactivated");

        if (await HasAssociatedEntriesAsync(id))
            throw new InvalidOperationException("Cannot deactivate account: account has associated journal entries");

        var before = MapToResponse(account);
        await DeactivateCascadeAsync(account);
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var after = MapToResponse(account);
        await _audit.LogAsync(userId, tenantId, "DEACTIVATE", "chart_of_accounts", account.Id, ipAddress, userAgent, before, after);

        return after;
    }

    private async Task<bool> HasAssociatedEntriesAsync(Guid accountId)
    {
        // TODO: Check journal_entry_lines table when FIN-2 is implemented
        // return await _db.Set<JournalEntryLine>()
        //     .AnyAsync(l => l.AccountId == accountId);
        await Task.CompletedTask;
        return false;
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
