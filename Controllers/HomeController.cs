using System.Diagnostics;
using Courses.Models;
using Courses.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Courses.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<User> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager)
        {
            _logger = logger;
            _userManager = userManager;
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

            // Сохраняем текущий путь для отображения в случае ошибки
            model.ExistingAvatarPath = user.AvatarPath;

            // Проверка файла аватара
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                // Проверка размера (5MB максимум)
                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarFile", "Максимальный размер файла - 5MB");
                }

                // Проверка расширения
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("AvatarFile", "Допустимые форматы: JPG, JPEG, PNG, GIF");
                }
            }

            if (ModelState.IsValid)
            {
                // Обработка аватара (только если файл был загружен)
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                    // Создаём папку если её нет
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

                    // Удаляем старый аватар если он существует
                    if (!string.IsNullOrEmpty(user.AvatarPath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            user.AvatarPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    // Обновляем путь к аватару
                    user.AvatarPath = $"/avatars/{uniqueFileName}";
                }

                // Обновляем телефон
                user.PhoneNumber = model.PhoneNumber;

                // Сохраняем изменения в базе данных
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
        public IActionResult Student()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
