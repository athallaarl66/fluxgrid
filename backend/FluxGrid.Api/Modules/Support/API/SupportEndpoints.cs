using System.Security.Claims;
using FluxGrid.Api.Modules.Support.Domain;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Support.API;

public static class SupportEndpoints
{
    public static void MapSupportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/support/contact", async (
            SupportTicketRequest request,
            HttpContext http,
            AppDbContext db) =>
        {
            var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return Results.Json(new { message = "Name is required (max 100 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 255)
                return Results.Json(new { message = "Email is required (max 255 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 200)
                return Results.Json(new { message = "Subject is required (max 200 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 2000)
                return Results.Json(new { message = "Message is required (max 2000 chars)." }, statusCode: 400);

            var ticket = new SupportTicket
            {
                UserId = userId,
                Name = request.Name,
                Email = request.Email,
                Subject = request.Subject,
                Message = request.Message
            };

            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = ticket.Id.ToString(),
                message = "Support ticket submitted successfully."
            });
        })
        .RequireAuthorization();
    }
}

public record SupportTicketRequest(
    string Name,
    string Email,
    string Subject,
    string Message
);
