using Courses.Data;
using Courses.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Courses.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(int notificationId)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.Type == NotificationType.CourseInvitation);

            if (notification == null || notification.CourseId == null)
                return NotFound();

            // Проверяем, не добавлен ли уже студент на курс
            bool alreadyEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId && uc.CourseId == notification.CourseId);

            if (!alreadyEnrolled)
            {
                _context.UserCourses.Add(new UserCourse
                {
                    UserId = userId,
                    CourseId = notification.CourseId.Value,
                    // Добавьте другие нужные поля, если есть
                });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Можно добавить TempData для сообщения об успехе
            TempData["SuccessMessage"] = "Вы успешно присоединились к курсу!";
            return RedirectToAction("Course", "Student");
        }
    }
} 