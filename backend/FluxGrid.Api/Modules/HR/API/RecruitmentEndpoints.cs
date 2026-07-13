using System.Text.Json;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.HR.API;

public static class RecruitmentEndpoints
{
    public static void MapRecruitmentEndpoints(this WebApplication app)
    {
        var recruitment = app.MapGroup("/api/v1/hr/recruitment");

        recruitment.MapPost("/upload-url", async (
            UploadUrlRequest request,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            try
            {
                var result = await service.RequestUploadUrlAsync(request, tenantId, userId);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                var statusCode = ex.Message.StartsWith("A candidate with this file")
                    ? 409 : 400;
                return Results.Problem(ex.Message, statusCode: statusCode);
            }
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapPost("/candidates", async (
            CreateCandidateRequest request,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var candidate = await service.CreateCandidateAsync(request, tenantId, userId, ip, ua);
                return Results.Created($"/api/v1/hr/recruitment/candidates/{candidate.Id}", candidate);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapGet("/candidates", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetCandidatesAsync(tenantId, search, status, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapGet("/candidates/{id:guid}", async (
            Guid id,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var candidate = await service.GetCandidateDetailAsync(id, tenantId);
            return candidate is null ? Results.NotFound() : Results.Ok(candidate);
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapPut("/candidates/{id:guid}/approve", async (
            Guid id,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.ApproveCandidateAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapPut("/candidates/{id:guid}/reject", async (
            Guid id,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.RejectCandidateAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapPost("/parse-webhook", async (
            HttpContext http,
            CvParsingService cvParsing) =>
        {
            using var reader = new StreamReader(http.Request.Body);
            var body = await reader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(body);

            if (!data.TryGetProperty("candidateId", out var cid) ||
                !data.TryGetProperty("tenantId", out var tid) ||
                !data.TryGetProperty("userId", out var uid))
                return Results.Problem("Missing required fields: candidateId, tenantId, userId", statusCode: 400);

            await cvParsing.ParseCandidateAsync(
                cid.GetGuid(), uid.GetGuid(), tid.GetGuid());

            return Results.Ok(new { status = "parsing_initiated" });
        })
        .AddEndpointFilter<QStashSignatureFilter>();
    }

    private static (Guid tenantId, Guid userId, string? ip, string? ua) GetAuditContext(HttpContext http)
    {
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

        var tenantClaim = http.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tid)) tenantId = tid;

        var userClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userClaim, out var uid)) userId = uid;

        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        return (tenantId, userId, ip, ua);
    }
}
