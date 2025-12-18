using Courses.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Courses.Filters
{
    public class NotificationCountActionFilter : IAsyncActionFilter
    {
        private readonly INotificationService _notificationService;

        public NotificationCountActionFilter(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var unreadCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);
                    context.HttpContext.Items["UnreadNotificationsCount"] = unreadCount;
                    
                    // Устанавливаем ViewBag для использования в представлениях
                    if (context.Controller is Controller controller)
                    {
                        controller.ViewBag.UnreadNotificationsCount = unreadCount;
                    }
                }
            }

            await next();
        }
    }
}

