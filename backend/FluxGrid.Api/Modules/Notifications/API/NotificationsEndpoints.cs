using System.Security.Claims;
using FluxGrid.Api.Modules.Notifications.Domain;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Notifications.API;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notifications/unread", async (
            HttpContext http,
            INotificationService notifService) =>
        {
            var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var notifs = await notifService.GetUnreadAsync(userId);
            var count = await notifService.GetUnreadCountAsync(userId);

            return Results.Ok(new
            {
                count,
                notifications = notifs.Select(n => new
                {
                    id = n.Id.ToString(),
                    type = n.Type,
                    title = n.Title,
                    body = n.Body,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                })
            });
        })
        .RequireAuthorization();

        app.MapPut("/api/notifications/{id:guid}/read", async (
            Guid id,
            HttpContext http,
            INotificationService notifService) =>
        {
            var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            await notifService.MarkReadAsync(userId, id);
            return Results.Ok(new { message = "Notification marked as read." });
        })
        .RequireAuthorization();

        app.MapPut("/api/notifications/read-all", async (
            HttpContext http,
            INotificationService notifService) =>
        {
            var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            await notifService.MarkAllReadAsync(userId);
            return Results.Ok(new { message = "All notifications marked as read." });
        })
        .RequireAuthorization();
    }
}
