using System.Text;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using FluxGrid.Api.Auth;
using FluxGrid.Api.Modules.Dashboard.API;
using FluxGrid.Api.Modules.Dashboard.Application;
using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.WMS.API;
using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Caching;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Minio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
    DotNetEnv.Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not configured");

var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
var storageBucketName = builder.Configuration["Storage:BucketName"] ?? "fluxgrid-cvs";

if (storageProvider == "S3")
{
    var storageEndpoint = builder.Configuration["Storage:Endpoint"] ?? throw new InvalidOperationException("Storage:Endpoint not configured");
    var storageAccessKey = builder.Configuration["Storage:AccessKey"] ?? throw new InvalidOperationException("Storage:AccessKey not configured");
    var storageSecretKey = builder.Configuration["Storage:SecretKey"] ?? throw new InvalidOperationException("Storage:SecretKey not configured");
    var storageUseSsl = bool.TryParse(builder.Configuration["Storage:UseSsl"], out var ssl) && ssl;

    builder.Services.AddSingleton<IMinioClient>(_ =>
        new MinioClient()
            .WithEndpoint(storageEndpoint)
            .WithCredentials(storageAccessKey, storageSecretKey)
            .WithSSL(storageUseSsl)
            .Build());
    builder.Services.AddSingleton<IFileStorageService>(sp =>
        new S3FileStorageService(sp.GetRequiredService<IMinioClient>(), storageUseSsl));
}
else
{
    builder.Services.AddSingleton<LocalFileStorageService>();
    builder.Services.AddSingleton<IFileStorageService>(sp =>
        sp.GetRequiredService<LocalFileStorageService>());
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in FluxGrid.Api.Shared.RBAC.Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim("permissions", permission) ||
                context.User.IsInRole("Admin")));
    }
});

var corsOrigins = builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.HttpStatusCode = 429;
    options.GeneralRules =
    [
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Limit = 5,
            Period = "1m"
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/v1/hr/payroll/calculate",
            Limit = 10,
            Period = "1m"
        },
        new RateLimitRule
        {
            Endpoint = "put:/api/v1/hr/payroll/*/finalize",
            Limit = 10,
            Period = "1m"
        },
        new RateLimitRule
        {
            Endpoint = "put:/api/v1/hr/payroll/*/recalculate",
            Limit = 10,
            Period = "1m"
        }
    ];
});
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<DomainEventDispatcher>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChartOfAccountService>();
builder.Services.AddScoped<JournalEntryService>();
builder.Services.AddScoped<PeriodService>();
builder.Services.AddScoped<PeriodValidator>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<FinanceDashboardService>();
builder.Services.AddScoped<StockLedgerService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<PurchaseReceiptService>();
builder.Services.AddScoped<SalesOrderService>();
builder.Services.AddScoped<PickListService>();
builder.Services.AddScoped<ShipmentService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<OrgChartService>();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<RecruitmentService>();
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var client = new HttpClient();
    client.BaseAddress = new Uri(config["TaskApp:BaseUrl"] ?? "http://localhost:4000");
    client.Timeout = TimeSpan.FromSeconds(30);
    return client;
});
builder.Services.AddScoped<AuditService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseCors("Frontend");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapChartOfAccountEndpoints();
app.MapJournalEntryEndpoints();
app.MapPeriodEndpoints();
app.MapReportEndpoints();
app.MapBudgetEndpoints();
app.MapFinanceDashboardEndpoints();
app.MapStockLedgerEndpoints();
app.MapInventoryBalanceEndpoints();
app.MapWmsDashboardEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapPurchaseReceiptEndpoints();
app.MapOutboundEndpoints();
app.MapHrEndpoints();
app.MapPayrollEndpoints();
app.MapRecruitmentEndpoints();

if (storageProvider != "S3")
{
    app.MapPut("/api/v1/hr/storage/{**objectKey}", async (
        string objectKey,
        HttpRequest request,
        LocalFileStorageService storage) =>
    {
        var path = storage.GetFilePath("fluxgrid-cvs", objectKey);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await using var stream = request.Body;
        await using var fileStream = File.Create(path);
        await stream.CopyToAsync(fileStream);
        return Results.Ok();
    });

    app.MapGet("/api/v1/hr/storage/{**objectKey}", async (
        string objectKey,
        LocalFileStorageService storage) =>
    {
        var path = storage.GetFilePath("fluxgrid-cvs", objectKey);
        if (!File.Exists(path)) return Results.NotFound();
        var ext = Path.GetExtension(path).ToLower();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
        var stream = File.OpenRead(path);
        return Results.File(stream, contentType, Path.GetFileName(path));
    });
}

app.Run();
