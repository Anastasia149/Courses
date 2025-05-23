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
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Courses.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public StudentController(AppDbContext context, INotificationService notificationService, UserManager<User> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
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

        public async Task<IActionResult> Certificates()
        {
            var userId = _userManager.GetUserId(User);
            var certificates = await _context.Certificates
                .Include(c => c.Course)
                .Include(c => c.Student)
                .Where(c => c.StudentId == userId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();

            return View(certificates);
        }

        public async Task<IActionResult> DownloadCertificate(int id)
        {
            var userId = _userManager.GetUserId(User);
            var certificate = await _context.Certificates
                .Include(c => c.Course)
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.Id == id && c.StudentId == userId);

            if (certificate == null)
                return NotFound();

            // Генерация PDF (пример с QuestPDF)
            var pdfBytes = GenerateCertificatePdf(certificate);

            var fileName = $"Certificate_{certificate.Course.Title}_{certificate.IssuedAt:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Пример генерации PDF с помощью QuestPDF
        private byte[] GenerateCertificatePdf(Certificate cert)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);

                    page.Content().Column(column =>
                    {
                        // Фон
                        column.Item().Background(Colors.LightBlue.Lighten5);

                        // Основной контент
                        column.Item().Padding(2, Unit.Centimetre).Column(content =>
                        {
                            // Верхняя декоративная линия
                            content.Item().PaddingBottom(20).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                            // Заголовок
                            content.Item().AlignCenter().Text("СЕРТИФИКАТ").FontSize(40).Bold().FontColor(Colors.Blue.Darken3);
                            content.Item().PaddingBottom(20).AlignCenter().Text("ОБ УСПЕШНОМ ОСВОЕНИИ КУРСА").FontSize(16).FontColor(Colors.Blue.Medium);

                            // Основной текст
                            content.Item().PaddingTop(40).AlignCenter().Text("Настоящий сертификат выдан").FontSize(14);
                            content.Item().PaddingTop(10).AlignCenter().Text(cert.Student.FullName).FontSize(24).Bold();
                            content.Item().PaddingTop(20).AlignCenter().Text("за успешное освоение курса").FontSize(14);
                            content.Item().PaddingTop(10).AlignCenter().Text(cert.Course.Title).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                            // Дата выдачи
                            content.Item().PaddingTop(40).AlignCenter().Text($"Дата выдачи: {cert.IssuedAt:dd.MM.yyyy}").FontSize(14);

                            // Нижняя декоративная линия
                            content.Item().PaddingTop(40).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                            // Подписи
                            content.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeItem().AlignCenter().Text("Директор").FontSize(12);
                                row.RelativeItem().AlignCenter().Text("Преподаватель").FontSize(12);
                            });
                        });

                        // Декоративные элементы в углах
                        column.Item().Row(row =>
                        {
                            // Верхний правый угол
                            row.RelativeItem().AlignRight().PaddingRight(2, Unit.Centimetre).PaddingTop(2, Unit.Centimetre)
                                .Width(100).Height(100).Background(Colors.Blue.Lighten5);

                            // Нижний левый угол
                            row.RelativeItem().AlignLeft().PaddingLeft(2, Unit.Centimetre).PaddingBottom(2, Unit.Centimetre)
                                .Width(100).Height(100).Background(Colors.Blue.Lighten5);
                        });
                    });

                    // Добавляем рамку
                    page.Footer().Border(1).BorderColor(Colors.Blue.Medium);
                });
            });

            return doc.GeneratePdf();
        }
    }
} 