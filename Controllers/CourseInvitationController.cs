using Courses.Data;
using Courses.Models;
using Courses.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courses.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class CourseInvitationController : Controller
    {
        private readonly AppDbContext _context;

        public CourseInvitationController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Invite(int courseId)
        {
            var course = _context.Courses.Find(courseId);
            if (course == null)
            {
                return NotFound();
            }

            // Проверяем, является ли текущий пользователь преподавателем курса
            if (course.TeacherId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
            {
                return Forbid();
            }

            var viewModel = new CourseInvitationViewModel
            {
                CourseId = courseId,
                CourseName = course.Title
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Invite(CourseInvitationViewModel viewModel)
        {
            var course = await _context.Courses.FindAsync(viewModel.CourseId);
            if (course == null)
            {
                return NotFound();
            }

            // Проверяем, является ли текущий пользователь преподавателем курса
            if (course.TeacherId != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
            {
                return Forbid();
            }

            // Ищем студента по email
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == viewModel.StudentEmail);
            if (student == null)
            {
                ModelState.AddModelError("StudentEmail", "Студент с таким email не найден");
                return View(viewModel);
            }

            // Проверяем, не является ли студент уже участником курса
            var isAlreadyEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == student.Id && uc.CourseId == viewModel.CourseId);
            if (isAlreadyEnrolled)
            {
                ModelState.AddModelError("StudentEmail", "Студент уже является участником курса");
                return View(viewModel);
            }

            // Проверяем, нет ли уже активного приглашения для этого студента
            var existingInvitation = await _context.Notifications
                .AnyAsync(n => n.UserId == student.Id 
                           && n.CourseId == viewModel.CourseId 
                           && n.Type == NotificationType.CourseInvitation
                           && !n.IsRead);
            if (existingInvitation)
            {
                ModelState.AddModelError("StudentEmail", "Студенту уже отправлено приглашение на этот курс");
                return View(viewModel);
            }

            // Создаем уведомление о приглашении
            var notification = new Notification
            {
                UserId = student.Id,
                Type = NotificationType.CourseInvitation,
                Title = "Приглашение на курс",
                Message = $"Вас пригласили на курс '{course.Title}'",
                CourseId = viewModel.CourseId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Приглашение успешно отправлено студенту {student.FullName}";
            return RedirectToAction("Course", "Home", new { id = viewModel.CourseId });
        }
    }
} 