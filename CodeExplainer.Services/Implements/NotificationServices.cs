using CodeExplainer.BusinessObject;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeExplainer.Services.Implements;

public class NotificationServices : INotificationServices
{
    private readonly ApplicationDbContext _context;
    
    public NotificationServices(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Notification> CreateNotificationAsync(Guid userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }
}