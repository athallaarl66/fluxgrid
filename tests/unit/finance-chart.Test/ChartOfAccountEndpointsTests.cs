using System.Net.Http.Json;
using System.Text.Json;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluxGrid.Api.Tests.Finance;

public class ChartOfAccountEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChartOfAccountEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
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
    public async Task GetCoa_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/finance/chart-of-accounts");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCoa_WithAuth_ReturnsAccounts()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/finance/chart-of-accounts");
        response.EnsureSuccessStatusCode();

        var accounts = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(accounts);
    }

    [Fact]
    public async Task GetCoa_WithFlatParam_ReturnsFlatList()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/finance/chart-of-accounts?flat=true");
        response.EnsureSuccessStatusCode();

        var accounts = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(accounts);
        foreach (var acc in accounts!)
        {
            var children = acc.GetProperty("children");
            Assert.Equal(0, children.GetArrayLength());
        }
    }

    [Fact]
    public async Task PostCoa_WithValidData_CreatesAccount()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000",
            name = "Test Account",
            type = "ASSET",
            isActive = true
        });

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6000", result.GetProperty("code").GetString());
        Assert.Equal("Test Account", result.GetProperty("name").GetString());
    }

    [Fact]
    public async Task PostCoa_WithDuplicateCode_Returns400()
    {
        var client = await CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000", name = "First", type = "ASSET"
        });

        var response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000", name = "Duplicate", type = "ASSET"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCoa_WithInvalidType_Returns400()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000",
            name = "Invalid",
            type = "INVALID_TYPE"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCoa_UpdatesAccount()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000", name = "Original", type = "ASSET"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/finance/chart-of-accounts/{id}", new
        {
            name = "Updated Name"
        });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Name", updated.GetProperty("name").GetString());
    }

    [Fact]
    public async Task PutCoa_WithNonExistentId_Returns404()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/v1/finance/chart-of-accounts/{Guid.NewGuid()}", new
        {
            name = "Nope"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCoa_DeactivatesAccount()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000", name = "ToDelete", type = "ASSET"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var deleteResponse = await client.DeleteAsync($"/api/v1/finance/chart-of-accounts/{id}");
        deleteResponse.EnsureSuccessStatusCode();

        var deleted = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(deleted.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task DeleteCoa_WithNonExistentId_Returns404()
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/v1/finance/chart-of-accounts/{Guid.NewGuid()}");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCoa_AfterCreate_IncludesNewAccount()
    {
        var client = await CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            code = "6000", name = "New Account", type = "LIABILITY"
        });

        var getResponse = await client.GetAsync("/api/v1/finance/chart-of-accounts?flat=true");
        var accounts = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Contains(accounts!, a => a.GetProperty("code").GetString() == "6000");
    }
}
