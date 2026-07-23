using System.Text.Json;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.HR.API;

public static class RecruitmentEndpoints
{
    public static void MapRecruitmentEndpoints(this WebApplication app)
    {
        var recruitment = app.MapGroup("/api/v1/hr/recruitment");
        var jobs = app.MapGroup("/api/v1/hr/recruitment/jobs");

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
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetCandidatesAsync(tenantId, search, status, page ?? 1, pageSize ?? 20);
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

        recruitment.MapPut("/candidates/{id:guid}", async (
            Guid id,
            CandidateUpdateRequest request,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var result = await service.UpdateCandidateAsync(id, request, tenantId, userId, ip, ua);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapGet("/candidates/{id:guid}/activities", async (
            Guid id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ActivityLogService activityLog,
            HttpContext http) =>
        {
            var tenantId = Guid.Empty;
            var tenantClaim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out var tid)) tenantId = tid;

            var result = await activityLog.GetActivitiesAsync(id, tenantId, page ?? 1, pageSize ?? 20);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrRecruitmentManage);

        recruitment.MapPost("/candidates/{id:guid}/activities", async (
            Guid id,
            AddNoteRequest request,
            ActivityLogService activityLog,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            await activityLog.LogAsync(id, ActivityAction.NoteAdded, userId, new { note = request.Note });
            return Results.Ok(new { success = true });
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

        recruitment.MapDelete("/candidates/{id:guid}", async (
            Guid id,
            RecruitmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                await service.DeleteCandidateAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(new { deleted = true });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 404);
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

        // ─── Job Posting Endpoints ────────────────────────────────────────

        jobs.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetListAsync(tenantId, search, status, page ?? 1, pageSize ?? 20);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrJobRead);

        jobs.MapPost("/", async (
            CreateJobRequest request,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var job = await service.CreateAsync(request, tenantId, userId, ip, ua);
                return Results.Created($"/api/v1/hr/recruitment/jobs/{job.Id}", job);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobManage);

        jobs.MapGet("/{id:guid}", async (
            Guid id,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var job = await service.GetByIdAsync(id, tenantId);
            return job is null ? Results.NotFound() : Results.Ok(job);
        })
        .RequireAuthorization(Permissions.HrJobRead);

        jobs.MapPut("/{id:guid}", async (
            Guid id,
            UpdateJobRequest request,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var job = await service.UpdateAsync(id, request, tenantId, userId, ip, ua);
                return job is null ? Results.NotFound() : Results.Ok(job);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobManage);

        jobs.MapDelete("/{id:guid}", async (
            Guid id,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var deleted = await service.DeleteAsync(id, tenantId, userId, ip, ua);
                return deleted ? Results.Ok(new { deleted = true }) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobManage);

        jobs.MapPost("/{id:guid}/publish", async (
            Guid id,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.PublishAsync(id, tenantId, userId, ip, ua);
                return result.Status == "PUBLISHED" ? Results.Ok(result) : Results.Problem(result.Message, statusCode: 503);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobManage);

        jobs.MapPost("/{id:guid}/close", async (
            Guid id,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.CloseAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobManage);

        jobs.MapGet("/{id:guid}/matches", async (
            Guid id,
            [FromQuery] double? minScore,
            [FromQuery] int? limit,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            try
            {
                var result = await service.GetJobMatchesAsync(id, tenantId, minScore, limit);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrJobRead);

        jobs.MapPost("/{jobId:guid}/matches/{candidateId:guid}/reasoning", async (
            Guid jobId,
            Guid candidateId,
            JobPostingService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetMatchReasoningAsync(jobId, candidateId, tenantId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrJobRead);
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
