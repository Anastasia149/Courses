using Courses.Models;

namespace Courses.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task<int> GetUnreadNotificationsCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task CreateNotificationAsync(string userId, string message);
        Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, int? relatedId = null);
    }
} 