using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class JournalEntryService
{
    private readonly AppDbContext _context;

    public JournalEntryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetListAsync(Guid tenantId, string? status, int page, int pageSize)
    {
        var query = _context.JournalEntries
            .Include(je => je.Lines)
            .Where(je => je.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(je => je.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(je => je.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<JournalEntry?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _context.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.Id == id && je.TenantId == tenantId);
    }

    public async Task<JournalEntry> CreateAsync(Guid tenantId, CreateJournalEntryRequest request, Guid userId)
    {
        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (request.Status != "DRAFT" && totalDebit != totalCredit)
        {
            throw new InvalidOperationException("Debit and Credit must be equal for SUBMITTED entries.");
        }

        string finalStatus = request.Status;
        if (request.Status != "DRAFT")
        {
            finalStatus = totalDebit > 50000000m ? "PENDING_APPROVAL" : "POSTED";
        }

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryNo = "JE-" + DateTime.UtcNow.Ticks,
            TransactionDate = request.TransactionDate,
            Description = request.Description,
            Status = finalStatus,
            TotalAmount = totalDebit,
            CreatedBy = userId,
            Lines = request.Lines.Select(l => new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description
            }).ToList()
        };

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();

        return entry;
    }

    public async Task<JournalEntry> UpdateDraftAsync(Guid id, Guid tenantId, UpdateJournalEntryRequest request)
    {
        var entry = await _context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (entry == null) throw new InvalidOperationException("Entry not found");
        if (entry.Status == "POSTED") throw new InvalidOperationException("Cannot update POSTED entry");

        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (request.Status != "DRAFT" && totalDebit != totalCredit)
        {
            throw new InvalidOperationException("Debit and Credit must be equal for SUBMITTED entries.");
        }

        string finalStatus = request.Status;
        if (request.Status != "DRAFT")
        {
            finalStatus = totalDebit > 50000000m ? "PENDING_APPROVAL" : "POSTED";
        }

        entry.TransactionDate = request.TransactionDate;
        entry.Description = request.Description;
        entry.Status = finalStatus;
        entry.TotalAmount = totalDebit;

        // Remove existing lines from DbSet directly
        var linesToRemove = await _context.JournalEntryLines
            .Where(l => l.EntryId == entry.Id)
            .ToListAsync();
        _context.JournalEntryLines.RemoveRange(linesToRemove);

        // Add new lines
        foreach (var l in request.Lines)
        {
            _context.JournalEntryLines.Add(new JournalEntryLine
            {
                EntryId = entry.Id,
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description
            });
        }

        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry> ApproveAsync(Guid id, Guid tenantId, Guid approverId)
    {
        var entry = await _context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (entry == null) throw new InvalidOperationException("Entry not found");
        if (entry.Status != "PENDING_APPROVAL") throw new InvalidOperationException("Entry is not pending approval");
        if (entry.CreatedBy == approverId) throw new InvalidOperationException("SELF_APPROVAL_DENIED");

        entry.Status = "POSTED";
        entry.ApprovedBy = approverId;
        await _context.SaveChangesAsync();

        return entry;
    }

    public async Task DeleteDraftAsync(Guid id, Guid tenantId)
    {
        var entry = await _context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (entry == null) throw new InvalidOperationException("Entry not found");
        if (entry.Status == "POSTED") throw new InvalidOperationException("CANNOT_VOID_POSTED");

        entry.Status = "VOID";
        await _context.SaveChangesAsync();
    }
}
