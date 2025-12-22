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
        private readonly IWebHostEnvironment _environment;

        public StudentController(AppDbContext context, INotificationService notificationService, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Course()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);

            // Принудительно проверяем и очищаем неверный путь к аватару
            if (user != null)
            {
                bool needsUpdate = false;
                if (string.IsNullOrWhiteSpace(user.AvatarPath))
                {
                    // Если путь пустой, убеждаемся, что он null
                    if (user.AvatarPath != null)
                    {
                        user.AvatarPath = null;
                        needsUpdate = true;
                    }
                }
                else if (!string.IsNullOrEmpty(user.Id))
                {
                    // Проверяем валидность пути
                    var fileName = Path.GetFileName(user.AvatarPath);
                    if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith(user.Id + "_", StringComparison.Ordinal))
                    {
                        // Путь неверный - очищаем его
                        user.AvatarPath = null;
                        needsUpdate = true;
                    }
                }
                
                if (needsUpdate)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            // Получаем уведомления
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            ViewBag.Notifications = notifications;
            ViewBag.User = user;
            ViewBag.UnreadNotificationsCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);

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
                        .Count(h => h.StudentId == userId && h.Status == HomeworkStatus.Pending),
                    CoverImagePath = uc.Course.CoverImagePath,
                    ProgressPercentage = uc.Course.Lessons
                        .Where(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video)
                        .Count() > 0 
                        ? (int)Math.Round((double)uc.Course.Lessons
                            .Where(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video)
                            .Count(l => l.Homeworks.Any(h => h.StudentId == userId && h.Status == HomeworkStatus.Approved)) 
                            / uc.Course.Lessons
                                .Where(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video)
                                .Count() * 100)
                        : 0
                })
                .ToListAsync();

            return View(new StudentCoursesViewModel { EnrolledCourses = enrolledCourses });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveCourse(int courseId)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var link = await _context.UserCourses
                .FirstOrDefaultAsync(uc => uc.CourseId == courseId && uc.UserId == userId);

            if (link == null)
            {
                TempData["ErrorMessage"] = "Вы не записаны на этот курс.";
                return RedirectToAction(nameof(Course));
            }

            _context.UserCourses.Remove(link);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вы покинули курс.";
            return RedirectToAction(nameof(Course));
        }

        public async Task<IActionResult> CourseDetails(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);
            
            // Принудительно проверяем и очищаем неверный путь к аватару
            if (user != null)
            {
                bool needsUpdate = false;
                if (string.IsNullOrWhiteSpace(user.AvatarPath))
                {
                    if (user.AvatarPath != null)
                    {
                        user.AvatarPath = null;
                        needsUpdate = true;
                    }
                }
                else if (!string.IsNullOrEmpty(user.Id))
                {
                    var fileName = Path.GetFileName(user.AvatarPath);
                    if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith(user.Id + "_", StringComparison.Ordinal))
                    {
                        user.AvatarPath = null;
                        needsUpdate = true;
                    }
                }
                
                if (needsUpdate)
                {
                    await _userManager.UpdateAsync(user);
                }
            }
            
            ViewBag.User = user;
            ViewBag.UnreadNotificationsCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            
            var userCourse = await _context.UserCourses
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
                            .ThenInclude(l => l.Homeworks)
                .Include(uc => uc.Course)
                    .ThenInclude(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == id);

            if (userCourse == null)
            {
                return NotFound();
            }

            var courseData = userCourse.Course;
            var allLessons = courseData.Lessons.OrderBy(l => l.Order).ToList();
            // Учитываем только уроки, где можно прикреплять файлы (Assignment и Video)
            var lessonsWithHomework = allLessons.Where(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video).ToList();
            var completedLessonsCount = lessonsWithHomework.Count(l => 
                l.Homeworks.Any(h => h.StudentId == userId && h.Status == HomeworkStatus.Approved));
            var progressPercentage = lessonsWithHomework.Count > 0 
                ? (int)Math.Round((double)completedLessonsCount / lessonsWithHomework.Count * 100) 
                : 0;

            // Получаем отзывы для курса
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.CourseId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var userReview = reviews.FirstOrDefault(r => r.UserId == userId);

            // Группируем уроки по модулям
            var modules = courseData.Modules
                .OrderBy(m => m.OrderNumber)
                .Select(m => new StudentModuleViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    OrderNumber = m.OrderNumber,
                    Lessons = m.Lessons
                        .OrderBy(l => l.Order)
                        .Select(l => new StudentLessonViewModel
                        {
                            Id = l.Id,
                            Title = l.Title,
                            Description = l.Content,
                            Order = l.Order,
                            ModuleId = l.ModuleId,
                            HasHomework = l.Type == Models.LessonType.Assignment || l.Type == Models.LessonType.Video,
                            HomeworkStatus = l.Homeworks
                                .Where(h => h.StudentId == userId)
                                .Select(h => h.Status)
                                .FirstOrDefault(),
                            IsCompleted = l.Homeworks.Any(h => h.StudentId == userId && h.Status == HomeworkStatus.Approved),
                            IsViewed = l.Homeworks.Any(h => h.StudentId == userId),
                            DueDate = null, // DueDate не реализован в модели Homework
                            Files = GetLessonFiles(l.Id),
                            LessonType = l.Type
                        })
                        .ToList(),
                    TotalLessons = m.Lessons.Count(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video),
                    CompletedLessons = m.Lessons
                        .Where(l => l.Type == LessonType.Assignment || l.Type == LessonType.Video)
                        .Count(l => l.Homeworks.Any(h => h.StudentId == userId && h.Status == HomeworkStatus.Approved))
                })
                .ToList();

            // Уроки без модуля
            var lessonsWithoutModule = allLessons
                .Where(l => l.ModuleId == null)
                .Select(l => new StudentLessonViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Content,
                    Order = l.Order,
                    ModuleId = null,
                    HasHomework = l.Type == Models.LessonType.Assignment || l.Type == Models.LessonType.Video,
                    HomeworkStatus = l.Homeworks
                        .Where(h => h.StudentId == userId)
                        .Select(h => h.Status)
                        .FirstOrDefault(),
                    IsCompleted = l.Homeworks.Any(h => h.StudentId == userId && h.Status == HomeworkStatus.Approved),
                    IsViewed = l.Homeworks.Any(h => h.StudentId == userId),
                    DueDate = null, // DueDate не реализован в модели Homework
                    Files = GetLessonFiles(l.Id),
                    LessonType = l.Type
                })
                .ToList();

            var course = new StudentCourseDetailsViewModel
            {
                CourseId = userCourse.CourseId,
                Title = courseData.Title,
                Description = courseData.Description,
                TeacherName = courseData.Teacher.FullName,
                CoverImagePath = courseData.CoverImagePath,
                AverageRating = averageRating,
                TotalReviews = reviews.Count,
                ProgressPercentage = progressPercentage,
                Modules = modules,
                Lessons = lessonsWithoutModule
            };

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = averageRating;
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.HasUserReview = userReview != null;
            ViewBag.UserReview = userReview;

            return View(course);
        }

        public async Task<IActionResult> Lesson(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);
            
            ViewBag.User = user;
            ViewBag.UnreadNotificationsCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);

            // Получаем урок с курсом
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(l => l.Module)
                .Include(l => l.Homeworks)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            // Проверяем, записан ли студент на курс
            var isEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId && uc.CourseId == lesson.CourseId);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "Вы должны быть записаны на курс, чтобы просматривать уроки.";
                return RedirectToAction("Course");
            }

            // Получаем домашнее задание студента для этого урока (исключаем отмененные и пустые)
            // Пустые задания (созданные автоматически для комментариев) не считаются отправленными
            var homework = lesson.Homeworks
                .Where(h => h.StudentId == userId && 
                           h.Status != HomeworkStatus.Cancelled &&
                           !string.IsNullOrWhiteSpace(h.Answer))
                .OrderByDescending(h => h.SubmittedAt)
                .FirstOrDefault();

            // Получаем предыдущий и следующий уроки
            var allLessons = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.Order)
                .ToListAsync();

            var currentIndex = allLessons.FindIndex(l => l.Id == id);
            var previousLesson = currentIndex > 0 ? allLessons[currentIndex - 1] : null;
            var nextLesson = currentIndex < allLessons.Count - 1 ? allLessons[currentIndex + 1] : null;

            var lessonViewModel = new StudentLessonViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Content,
                Order = lesson.Order,
                ModuleId = lesson.ModuleId,
                HasHomework = lesson.Type == Models.LessonType.Assignment || lesson.Type == Models.LessonType.Video,
                HomeworkStatus = homework != null ? homework.Status : HomeworkStatus.Pending,
                IsCompleted = homework?.Status == HomeworkStatus.Approved,
                // IsViewed = true только если задание реально отправлено (не пустое)
                IsViewed = homework != null && 
                          homework.Status != HomeworkStatus.Cancelled && 
                          !string.IsNullOrWhiteSpace(homework.Answer),
                DueDate = null,
                Files = GetLessonFiles(lesson.Id),
                LessonType = lesson.Type
            };

            ViewBag.CourseId = lesson.CourseId;
            ViewBag.CourseTitle = lesson.Course.Title;
            ViewBag.PreviousLessonId = previousLesson?.Id;
            ViewBag.NextLessonId = nextLesson?.Id;
            ViewBag.ModuleTitle = lesson.Module?.Title;
            ViewBag.LessonType = lesson.Type;

            return View(lessonViewModel);
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
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;
            ViewBag.UnreadNotificationsCount = await _notificationService.GetUnreadNotificationsCountAsync(userId);
            
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

        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                return BadRequest("Файл не выбран");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (fileExtension == ".gif" || avatarFile.ContentType.ToLowerInvariant() == "image/gif")
            {
                ModelState.AddModelError("AvatarFile", "GIF-изображения не поддерживаются для аватаров. Загрузите JPG или PNG.");
            }
            else if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("AvatarFile", "Разрешены форматы: JPG, JPEG, PNG");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            user.AvatarPath = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            return Ok(new { avatarPath = user.AvatarPath });
        }
    }
} 