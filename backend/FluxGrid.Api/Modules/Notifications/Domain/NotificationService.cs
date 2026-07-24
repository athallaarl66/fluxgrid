using FluxGrid.Api.Modules.Notifications.Domain;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Notifications.Domain;

public interface INotificationService
{
    Task<Notification> CreateAsync(Guid userId, string type, string title, string body);
    Task<List<Notification>> GetUnreadAsync(Guid userId, int limit = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkReadAsync(Guid userId, Guid notificationId);
    Task MarkAllReadAsync(Guid userId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task<Notification> CreateAsync(Guid userId, string type, string title, string body)
    {
        var notif = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body
        };
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();
        return notif;
    }

    public async Task<List<Notification>> GetUnreadAsync(Guid userId, int limit = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId)
    {
        var notif = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notif is not null)
        {
            notif.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread)
            n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
