using System.Diagnostics;
using Courses.Data;
using Courses.Models;
using Courses.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courses.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<User> userManager,
            AppDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Course()
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var courses = await _context.Courses
                    .Include(c => c.Lessons)
                    .ThenInclude(l => l.Homeworks)
                    .Where(c => c.TeacherId == teacherId)
                    .ToListAsync();

                var model = new TeacherCoursesViewModel
                {
                    Courses = courses
                };

                if (courses.Any())
                {
                    var firstCourse = courses.First();

                    model.SelectedCourse = new CourseDetailsViewModel
                    {
                        Course = firstCourse,
                        EnrolledStudentsCount = await _context.UserCourses
                            .CountAsync(uc => uc.CourseId == firstCourse.Id),
                        PendingHomeworks = await _context.Homeworks
                            .Include(h => h.Student)
                            .Include(h => h.Lesson)
                            .Where(h => h.Lesson.CourseId == firstCourse.Id &&
                                       h.Status == HomeworkStatus.Pending)
                            .ToListAsync()
                    };
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении курса");
                return StatusCode(500, "Произошла ошибка при получении курса");
            }
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Teacher()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var model = new TeacherProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ExistingAvatarPath = user.AvatarPath
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Teacher(TeacherProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Устанавливаем текущий аватар для отображения
            model.ExistingAvatarPath = user.AvatarPath;

            // Проверяем файл аватара
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                // Проверяем размер файла (максимум 5MB)
                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarFile", "Максимальный размер файла - 5MB");
                }

                // Проверяем расширение только для аватаров
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                if (fileExtension == ".gif" || model.AvatarFile.ContentType.ToLowerInvariant() == "image/gif")
                {
                    ModelState.AddModelError("AvatarFile", "GIF-изображения не поддерживаются для аватаров. Загрузите JPG или PNG.");
                }
                else if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("AvatarFile", "Разрешены форматы: JPG, JPEG, PNG");
                }
            }

            if (ModelState.IsValid)
            {
                // Сохраняем новый аватар (если был загружен)
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                    // Создаём папку, если её нет
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Уникальное имя файла
                    var uniqueFileName = $"{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.AvatarFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Сохраняем файл
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    // Удаляем старый аватар, если был
                    if (!string.IsNullOrEmpty(user.AvatarPath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            user.AvatarPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    // Сохраняем путь к новому аватару
                    user.AvatarPath = $"/avatars/{uniqueFileName}";
                }

                // Сохраняем телефон
                user.PhoneNumber = model.PhoneNumber;

                // Обновляем пользователя
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    // Если ошибка - добавляем в ModelState
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return View(model);
                }

                TempData["SuccessMessage"] = "Профиль успешно обновлён!";
                return RedirectToAction(nameof(Teacher));
            }

            // Если ModelState невалиден
            return View(model);
        }


        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Student()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var model = new StudentProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ExistingAvatarPath = user.AvatarPath
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Student(StudentProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Устанавливаем текущий аватар для отображения
            model.ExistingAvatarPath = user.AvatarPath;

            // Проверяем файл аватара
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                // Проверяем размер файла (максимум 5MB)
                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarFile", "Максимальный размер файла - 5MB");
                }

                // Проверяем расширение только для аватаров
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                if (fileExtension == ".gif" || model.AvatarFile.ContentType.ToLowerInvariant() == "image/gif")
                {
                    ModelState.AddModelError("AvatarFile", "GIF-изображения не поддерживаются для аватаров. Загрузите JPG или PNG.");
                }
                else if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("AvatarFile", "Разрешены форматы: JPG, JPEG, PNG");
                }
            }

            if (ModelState.IsValid)
            {
                // Сохраняем новый аватар (если был загружен)
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                    // Создаём папку, если её нет
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Уникальное имя файла
                    var uniqueFileName = $"{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.AvatarFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Сохраняем файл
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    // Удаляем старый аватар, если был
                    if (!string.IsNullOrEmpty(user.AvatarPath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            user.AvatarPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    // Сохраняем путь к новому аватару
                    user.AvatarPath = $"/avatars/{uniqueFileName}";
                }

                // Сохраняем телефон
                user.PhoneNumber = model.PhoneNumber;

                // Обновляем пользователя
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    // Если ошибка - добавляем в ModelState
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return View(model);
                }

                TempData["SuccessMessage"] = "Профиль успешно обновлён!";
                return RedirectToAction(nameof(Student));
            }

            // Если ModelState невалиден
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
