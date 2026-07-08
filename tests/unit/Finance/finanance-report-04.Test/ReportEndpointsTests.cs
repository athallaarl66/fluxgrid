using System.Net.Http.Json;
using System.Text.Json;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluxGrid.Api.Tests.Finance;

public class ReportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private Guid _cashBankId;

    public ReportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("SEED_ADMIN_PASSWORD", "admin123");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:SecretKey", "test-secret-key-that-is-at-least-32-characters-long-for-hs256");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test;Username=test;Password=test");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _cashBankId = Guid.NewGuid();
        var liabilityId = Guid.NewGuid();
        var revenueId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();

        db.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = Guid.NewGuid(), Code = "1000", Name = "Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = _cashBankId, Code = "1110", Name = "Cash in Bank", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = null },
            new ChartOfAccount { Id = liabilityId, Code = "2000", Name = "Liabilities", Type = AccountTypes.Liability, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = Guid.NewGuid(), Code = "3000", Name = "Equity", Type = AccountTypes.Equity, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = revenueId, Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = expenseId, Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId }
        );
        await db.SaveChangesAsync();

        // Create posted journal entry
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 15),
            Description = "Test entry",
            Status = "POSTED",
            TotalAmount = 10_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = _cashBankId, Debit = 10_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = liabilityId, Debit = 0, Credit = 10_000_000 }
        );

        // Create revenue entry for P&L
        var revEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 15),
            Description = "Revenue entry",
            Status = "POSTED",
            TotalAmount = 10_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        db.JournalEntries.Add(revEntry);
        await db.SaveChangesAsync();
        db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = _cashBankId, Debit = 10_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = revenueId, Debit = 0, Credit = 10_000_000 }
        );

        // Create expense entry for P&L
        var expEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 20),
            Description = "Expense entry",
            Status = "POSTED",
            TotalAmount = 6_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        db.JournalEntries.Add(expEntry);
        await db.SaveChangesAsync();
        db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = expenseId, Debit = 6_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = _cashBankId, Debit = 0, Credit = 6_000_000 }
        );

        // Create draft entry
        var draftEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 25),
            Description = "Draft entry",
            Status = "DRAFT",
            TotalAmount = 1_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        db.JournalEntries.Add(draftEntry);
        await db.SaveChangesAsync();
        db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = draftEntry.Id, AccountId = _cashBankId, Debit = 1_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = draftEntry.Id, AccountId = liabilityId, Debit = 0, Credit = 1_000_000 }
        );

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123"
        });
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task TrialBalance_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/finance/reports/trial-balance?startDate=2026-01-01&endDate=2026-12-31&includeDrafts=false");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TrialBalance_WithAuth_ReturnsReport()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/finance/reports/trial-balance?startDate=2026-06-01&endDate=2026-06-30&includeDrafts=false&_tenantId={_tenantId}");

        // Note: In a real scenario we'd need tenant context. Since InMemory DB
        // doesn't have real auth, this test verifies the endpoint accepts valid requests.
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(report.TryGetProperty("rows", out _));
    }

    [Fact]
    public async Task PL_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/finance/reports/pl?startDate=2026-01-01&endDate=2026-12-31&includeDrafts=false");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PL_WithAuth_ReturnsReport()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/finance/reports/pl?startDate=2026-06-01&endDate=2026-06-30&includeDrafts=false");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(report.TryGetProperty("rows", out _));
    }

    [Fact]
    public async Task BalanceSheet_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/finance/reports/balance-sheet?asOfDate=2026-06-30&includeDrafts=false");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BalanceSheet_WithAuth_ReturnsReport()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/finance/reports/balance-sheet?asOfDate=2026-06-30&includeDrafts=false&netIncome=4000000");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(report.TryGetProperty("rows", out _));
    }

    [Fact]
    public async Task Ledger_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/finance/reports/{Guid.NewGuid()}/ledger?startDate=2026-01-01&endDate=2026-12-31&includeDrafts=false&page=1&pageSize=20");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ledger_WithAuth_ReturnsRows()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/finance/reports/{_cashBankId}/ledger?startDate=2026-06-01&endDate=2026-06-30&includeDrafts=false&page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("rows", out _));
        Assert.True(result.TryGetProperty("total", out _));
    }
}
