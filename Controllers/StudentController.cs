using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Courses.Data;
using Courses.Models;
using Courses.ViewModels;
using Courses.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Courses.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public StudentController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Course()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Получаем уведомления
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            ViewBag.Notifications = notifications;

            // Получаем курсы студента
            var enrolledCourses = await _context.UserCourses
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                .Where(uc => uc.UserId == userId)
                .Select(uc => new StudentCourseViewModel
                {
                    CourseId = uc.CourseId,
                    Title = uc.Course.Title,
                    Description = uc.Course.Description,
                    TeacherName = uc.Course.Teacher.FullName,
                    LessonsCount = uc.Course.Lessons.Count,
                    EnrolledAt = uc.EnrollmentDate,
                    PendingHomeworksCount = uc.Course.Lessons
                        .SelectMany(l => l.Homeworks)
                        .Count(h => h.Status == HomeworkStatus.Pending)
                })
                .ToListAsync();

            return View(new StudentCoursesViewModel { EnrolledCourses = enrolledCourses });
        }

        public async Task<IActionResult> CourseDetails(int id, int? lessonId = null)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userCourse = await _context.UserCourses
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == id);

            if (userCourse == null)
            {
                return NotFound();
            }

            var course = new StudentCourseDetailsViewModel
            {
                CourseId = userCourse.CourseId,
                Title = userCourse.Course.Title,
                Description = userCourse.Course.Description,
                TeacherName = userCourse.Course.Teacher.FullName,
                Lessons = userCourse.Course.Lessons
                    .OrderBy(l => l.Order)
                    .Select(l => new StudentLessonViewModel
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Description = l.Content,
                        Order = l.Order,
                        HasHomework = l.Homeworks.Any(h => h.StudentId == userId),
                        HomeworkStatus = l.Homeworks
                            .Where(h => h.StudentId == userId)
                            .Select(h => h.Status)
                            .FirstOrDefault()
                    })
                    .ToList()
            };

            // Добавляем файлы для каждого урока
            foreach (var lesson in course.Lessons)
            {
                lesson.Files = GetLessonFiles(lesson.Id);
            }

            // Выбор урока
            if (lessonId.HasValue)
                course.SelectedLesson = course.Lessons.FirstOrDefault(l => l.Id == lessonId.Value);
            else
                course.SelectedLesson = course.Lessons.OrderBy(l => l.Order).FirstOrDefault();

            return View(course);
        }

        private List<LessonFileViewModel> GetLessonFiles(int lessonId)
        {
            var path = Path.Combine("wwwroot", "uploads", "lessons", lessonId.ToString());
            if (!Directory.Exists(path))
                return new List<LessonFileViewModel>();

            return Directory.GetFiles(path)
                .Select(f => new LessonFileViewModel
                {
                    FileName = Path.GetFileName(f),
                    FilePath = "/uploads/lessons/" + lessonId + "/" + Path.GetFileName(f)
                }).ToList();
        }
    }
} 