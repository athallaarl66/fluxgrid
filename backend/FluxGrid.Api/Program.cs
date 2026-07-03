using System.Text;
using FluxGrid.Api.Auth;
using FluxGrid.Api.Modules.Dashboard.API;
using FluxGrid.Api.Modules.Dashboard.Application;
using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not configured");

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
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in FluxGrid.Api.Shared.RBAC.Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("permissions", permission));
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChartOfAccountService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapChartOfAccountEndpoints();

app.Run();
