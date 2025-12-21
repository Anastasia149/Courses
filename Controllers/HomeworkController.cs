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
                // Разрешаем обновление задания, если оно было отклонено или отменено
                // Также разрешаем обновление, если задание было создано автоматически для комментариев (пустой ответ)
                if (homework.Status != HomeworkStatus.Rejected && 
                    homework.Status != HomeworkStatus.Cancelled &&
                    !string.IsNullOrWhiteSpace(homework.Answer))
                {
                    TempData["Error"] = "Вы не можете отправить домашнее задание повторно, пока оно не отклонено или не отменено.";
                    return RedirectToAction("Lesson", "Student", new { id = lessonId });
                }
            }

            // Гарантируем, что answer не null (используем пустую строку вместо null)
            answer = answer ?? string.Empty;

            // Проверяем, что есть текстовый ответ
            if (string.IsNullOrWhiteSpace(answer))
            {
                TempData["Error"] = "Пожалуйста, введите текстовый ответ.";
                return RedirectToAction("Lesson", "Student", new { id = lessonId });
            }

            // Если файлы не пришли в параметре, получаем их из Request.Form.Files
            if (files == null || !files.Any())
            {
                files = Request.Form.Files?.ToList() ?? new List<IFormFile>();
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
                // Сохраняем ID для дальнейшего использования
                var homeworkId = homework.Id;
                
                // Удаляем старые файлы, если они есть (при обновлении задания)
                // Делаем это до обновления свойств homework
                if (homework.Files != null && homework.Files.Any())
                {
                    var filesToDelete = homework.Files.ToList();
                    foreach (var file in filesToDelete)
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        _context.Remove(file);
                    }
                }
                
                // Обновляем существующее домашнее задание
                homework.Answer = answer;
                homework.Status = HomeworkStatus.Pending;
                homework.SubmittedAt = DateTime.UtcNow;
                homework.Feedback = null;
            }

            // Обработка файлов
            if (files != null && files.Any())
            {
                var homeworkUploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "homeworks", homework.Id.ToString());
                if (!Directory.Exists(homeworkUploadsFolder))
                {
                    Directory.CreateDirectory(homeworkUploadsFolder);
                }

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        var filePath = Path.Combine(homeworkUploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        var relativePath = $"/uploads/homeworks/{homework.Id}/{uniqueFileName}";
                        var homeworkFile = new HomeworkFile
                        {
                            FileName = file.FileName,
                            FilePath = relativePath,
                            ContentType = file.ContentType,
                            FileSize = file.Length,
                            HomeworkId = homework.Id
                        };
                        homework.Files.Add(homeworkFile);
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                // Если произошла ошибка конкурентности, проверяем, не был ли homework уже сохранен
                var savedHomework = await _context.Homeworks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId);
                
                if (savedHomework != null && savedHomework.Answer == answer)
                {
                    // Домашнее задание уже успешно сохранено, просто используем его
                    homework = savedHomework;
                }
                else
                {
                    // Если не сохранено, пытаемся сохранить заново
                    // Отсоединяем все отслеживаемые сущности
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    
                    // Загружаем homework заново
                    homework = await _context.Homeworks
                        .Include(h => h.Files)
                        .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId);
                    
                    if (homework == null)
                    {
                        TempData["Error"] = "Домашнее задание было удалено. Пожалуйста, попробуйте отправить заново.";
                        return RedirectToAction("Lesson", "Student", new { id = lessonId });
                    }
                    
                    // Удаляем старые файлы, если они есть
                    if (homework.Files != null && homework.Files.Any())
                    {
                        var filesToDelete = homework.Files.ToList();
                        foreach (var file in filesToDelete)
                        {
                            var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            _context.Remove(file);
                        }
                    }
                    
                    // Обновляем данные заново
                    homework.Answer = answer;
                    homework.Status = HomeworkStatus.Pending;
                    homework.SubmittedAt = DateTime.UtcNow;
                    homework.Feedback = null;
                    
                    // Повторно обрабатываем файлы, если они есть
                    if (files != null && files.Any())
                    {
                        var homeworkUploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "homeworks", homework.Id.ToString());
                        if (!Directory.Exists(homeworkUploadsFolder))
                        {
                            Directory.CreateDirectory(homeworkUploadsFolder);
                        }

                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                                var filePath = Path.Combine(homeworkUploadsFolder, uniqueFileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                var relativePath = $"/uploads/homeworks/{homework.Id}/{uniqueFileName}";
                                var homeworkFile = new HomeworkFile
                                {
                                    FileName = file.FileName,
                                    FilePath = relativePath,
                                    ContentType = file.ContentType,
                                    FileSize = file.Length,
                                    HomeworkId = homework.Id
                                };
                                homework.Files.Add(homeworkFile);
                            }
                        }
                    }
                    
                    // Пробуем сохранить снова (тихо игнорируем ошибку, если данные уже сохранены)
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                    {
                        // Проверяем еще раз, не был ли homework сохранен
                        var finalCheck = await _context.Homeworks
                            .AsNoTracking()
                            .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId);
                        
                        if (finalCheck == null || finalCheck.Answer != answer)
                        {
                            // Действительно не сохранено - показываем ошибку
                            TempData["Error"] = "Произошла ошибка при сохранении. Пожалуйста, попробуйте еще раз.";
                            return RedirectToAction("Lesson", "Student", new { id = lessonId });
                        }
                        // Иначе - данные сохранены, продолжаем
                        homework = finalCheck;
                    }
                }
            }

            // Отправляем уведомление преподавателю
            await _notificationService.CreateNotificationAsync(
                lesson.Course.TeacherId,
                "Новое домашнее задание",
                $"Студент отправил домашнее задание по уроку '{lesson.Title}'",
                NotificationType.HomeworkSubmitted,
                homework.Id
            );

            TempData["Success"] = "Домашнее задание успешно отправлено!";
            return RedirectToAction("Lesson", "Student", new { id = lessonId });
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

            // Не возвращаем задание, если оно отменено или имеет пустой ответ (создано автоматически для комментариев)
            if (homework == null || 
                homework.Status == HomeworkStatus.Cancelled || 
                string.IsNullOrWhiteSpace(homework.Answer))
                return Json(null);

            return Json(new
            {
                homework.Id,
                homework.Answer,
                Status = homework.Status.ToString(),
                homework.Feedback,
                SubmittedAt = homework.SubmittedAt.ToString("o"), // ISO 8601 format
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

            // Путь к файлу уже содержит полный относительный путь
            var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
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