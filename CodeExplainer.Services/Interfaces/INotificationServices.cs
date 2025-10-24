using CodeExplainer.BusinessObject.Models;

namespace CodeExplainer.Services.Interfaces;

public interface INotificationServices
{
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId);
    Task<Notification> CreateNotificationAsync(Guid userId, string title, string message);
    Task MarkAsReadAsync(Guid notificationId);
    Task DeleteNotificationAsync(Guid notificationId);
}