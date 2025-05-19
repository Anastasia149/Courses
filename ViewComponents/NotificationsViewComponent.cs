using Microsoft.AspNetCore.Mvc;
using Courses.Services;
using System.Security.Claims;

namespace Courses.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationsViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return View(0);
            }

            var unreadCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            return View(unreadCount);
        }
    }
} 