using Courses.Data;
using Courses.Models;
using Microsoft.EntityFrameworkCore;

namespace Courses.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationsCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateNotificationAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = "Уведомление",
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, int? relatedId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // Устанавливаем связанные сущности в зависимости от типа уведомления
            switch (type)
            {
                case NotificationType.HomeworkSubmitted:
                case NotificationType.HomeworkGraded:
                    notification.HomeworkId = relatedId;
                    break;
                case NotificationType.CourseInvitation:
                    notification.CourseId = relatedId;
                    break;
                case NotificationType.NewLesson:
                    notification.LessonId = relatedId;
                    break;
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
} 