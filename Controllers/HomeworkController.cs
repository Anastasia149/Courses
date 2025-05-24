using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Courses.Data;
using Courses.Models;
using Courses.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Courses.Controllers
{
    [Authorize(Roles = "Student")]
    public class HomeworkController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;

        public HomeworkController(
            AppDbContext context,
            INotificationService notificationService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _notificationService = notificationService;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int lessonId, string answer, List<IFormFile> files)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Получаем урок для определения CourseId
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            
            if (lesson == null)
                return NotFound();

            // Проверяем, существует ли уже домашнее задание для этого урока
            var homework = await _context.Homeworks
                .Include(h => h.Files)
                .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId);

            if (homework != null)
            {
                // Проверяем, можно ли отправить домашнее задание повторно
                if (homework.Status != HomeworkStatus.Rejected && homework.Status != HomeworkStatus.Cancelled)
                {
                    TempData["Error"] = "Вы не можете отправить домашнее задание повторно, пока оно не отклонено или не отменено.";
                    return RedirectToAction("CourseDetails", "Student", new { id = lesson.CourseId, lessonId = lessonId });
                }
            }

            if (homework == null)
            {
                // Создаем новое домашнее задание
                homework = new Homework
                {
                    LessonId = lessonId,
                    StudentId = userId,
                    Answer = answer,
                    Status = HomeworkStatus.Pending,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.Homeworks.Add(homework);
            }
            else
            {
                // Обновляем существующее домашнее задание
                homework.Answer = answer;
                homework.Status = HomeworkStatus.Pending;
                homework.SubmittedAt = DateTime.UtcNow;
                homework.Feedback = null;
            }

            // Обработка файлов
            if (files != null && files.Any())
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "homework");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var homeworkFile = new HomeworkFile
                        {
                            FileName = file.FileName,
                            FilePath = uniqueFileName,
                            ContentType = file.ContentType,
                            FileSize = file.Length,
                            HomeworkId = homework.Id
                        };

                        homework.Files.Add(homeworkFile);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Отправляем уведомление преподавателю
            await _notificationService.CreateNotificationAsync(
                lesson.Course.TeacherId,
                "Новое домашнее задание",
                $"Студент отправил домашнее задание по уроку '{lesson.Title}'",
                NotificationType.HomeworkSubmitted,
                homework.Id
            );

            TempData["Success"] = "Домашнее задание успешно отправлено!";
            return RedirectToAction("CourseDetails", "Student", new { id = lesson.CourseId, lessonId = lessonId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int homeworkId)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var homework = await _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Course)
                .Include(h => h.Files)
                .FirstOrDefaultAsync(h => h.Id == homeworkId && h.StudentId == userId);

            if (homework == null)
                return NotFound();

            // Удаляем файлы из файловой системы
            if (homework.Files != null && homework.Files.Any())
            {
                foreach (var file in homework.Files.ToList())
                {
                    if (System.IO.File.Exists(file.FilePath))
                    {
                        System.IO.File.Delete(file.FilePath);
                    }
                    _context.Remove(file); // Удаляем запись из БД
                }
                await _context.SaveChangesAsync();
            }

            homework.Status = HomeworkStatus.Cancelled;
            await _context.SaveChangesAsync();

            // Отправляем уведомление преподавателю
            await _notificationService.CreateNotificationAsync(
                homework.Lesson.Course.TeacherId,
                "Отмена домашнего задания",
                $"Студент отменил отправку домашнего задания по уроку '{homework.Lesson.Title}'",
                NotificationType.HomeworkSubmitted,
                homework.Id
            );

            TempData["Success"] = "Домашнее задание отменено.";
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetHomework(int lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var homework = await _context.Homeworks
                .Include(h => h.Files)
                .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId);

            if (homework == null || homework.Status == HomeworkStatus.Cancelled)
                return Json(null);

            return Json(new
            {
                homework.Id,
                homework.Answer,
                homework.Status,
                homework.Feedback,
                homework.SubmittedAt,
                files = homework.Files.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileSize,
                    f.ContentType
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var file = await _context.HomeworkFiles
                .Include(f => f.Homework)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                return NotFound();
            }

            // Проверяем, имеет ли пользователь доступ к файлу
            if (file.Homework.StudentId != userId && 
                !await _context.UserCourses.AnyAsync(uc => 
                    uc.CourseId == file.Homework.Lesson.CourseId && 
                    uc.UserId == userId))
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "homework", file.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, file.ContentType, file.FileName);
        }
    }
} 