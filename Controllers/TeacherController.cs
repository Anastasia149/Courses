    using Courses.Data;
    using Courses.Models;
    using Courses.ViewModels;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using Courses.Services; 

    namespace Courses.Controllers
    {
        [Authorize(Roles = "Teacher")]
        public class TeacherController : Controller
        {
            private readonly AppDbContext _context;
            private readonly UserManager<User> _userManager;
            private readonly ILogger<TeacherController> _logger;
            private readonly IWebHostEnvironment _environment;
            private readonly INotificationService _notificationService;
            private readonly ICertificateService _certificateService;

        public TeacherController(
                AppDbContext context,
                UserManager<User> userManager,
                ILogger<TeacherController> logger,
                IWebHostEnvironment environment,
                INotificationService notificationService,
                ICertificateService certificateService)
            {
                _context = context;
                _userManager = userManager;
                _logger = logger;
                _environment= environment;
                _notificationService = notificationService;
                _certificateService = certificateService;
        }

        // Главная страница преподавателя
        public async Task<IActionResult> Index()
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
                    Courses = courses ?? new List<Course>(),
                    SelectedCourse = null // не загружаем детали, пока пользователь не выберет курс
                };

                return View("~/Views/Home/Course.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке курсов преподавателя");
                return StatusCode(500, "Произошла ошибка при загрузке данных");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateModule(int courseId)
        {
            var teacherId = _userManager.GetUserId(User);
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
            if (!courseExists) return NotFound();
            return View(new CreateModuleViewModel { CourseId = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(CreateModuleViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == model.CourseId && c.TeacherId == teacherId);
            if (course == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var module = new Module
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                OrderNumber = model.OrderNumber
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Модуль создан";
            return RedirectToAction(nameof(AddLesson), new { courseId = model.CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudentFromCourse(int courseId, string userId)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

                if (course == null)
                {
                    return NotFound("Курс не найден или доступ запрещен");
                }

                var userCourse = await _context.UserCourses
                    .FirstOrDefaultAsync(uc => uc.CourseId == courseId && uc.UserId == userId);

                if (userCourse == null)
                {
                    TempData["ErrorMessage"] = "Пользователь не найден в курсе";
                    return RedirectToAction(nameof(CourseDetails), new { id = courseId });
                }

                _context.UserCourses.Remove(userCourse);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Пользователь удален из курса";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении пользователя {userId} из курса {courseId}");
                TempData["ErrorMessage"] = "Произошла ошибка при удалении пользователя из курса";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }
        }

        // Детали курса с фильтрацией ДЗ (без пагинации)
        public async Task<IActionResult> CourseDetails(int id, HomeworkStatus? status = HomeworkStatus.Pending)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var courses = await _context.Courses
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Module)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                    .Where(c => c.TeacherId == teacherId)
                    .ToListAsync();

                var selectedCourse = await _context.Courses
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Module)
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                            .ThenInclude(h => h.Student)
                    .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

                if (selectedCourse == null)
                {
                    return NotFound();
                }

                // Фильтрация работ по статусу
                var homeworksQuery = selectedCourse.Lessons
                    .SelectMany(l => l.Homeworks);

                List<Homework> homeworks;
                if (status == null || status == HomeworkStatus.Pending)
                {
                    // Вкладка "Сданные": показываем ожидающие и уже проверенные
                    homeworks = homeworksQuery
                        .Where(h => h.Status == HomeworkStatus.Pending || h.Status == HomeworkStatus.Approved)
                        .ToList();
                }
                else
                {
                    homeworks = homeworksQuery
                        .Where(h => h.Status == status)
                        .ToList();
                }

                var enrolledUsers = await _context.UserCourses
                    .Include(uc => uc.User)
                    .Where(uc => uc.CourseId == id)
                    .ToListAsync();

                var model = new TeacherCoursesViewModel
                {
                    Courses = courses,
                    CurrentStatus = status,
                    SelectedCourse = new CourseDetailsViewModel
                    {
                        Course = selectedCourse,
                        PendingHomeworks = homeworks,
                        EnrolledStudentsCount = enrolledUsers.Count,
                        EnrolledStudents = enrolledUsers.Select(uc => uc.User).ToList(),
                        CurrentStatus = status // Сохраняем текущий статус фильтрации
                    }
                };

                return View("~/Views/Home/Course.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке курса {id}");
                return StatusCode(500, "Произошла ошибка при загрузке курса");
            }
        }


        // Проверка ДЗ (GET)
        public async Task<IActionResult> ReviewHomework(int homeworkId, string returnUrl = null)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // 🔍 Грузим ДЗ ТОЛЬКО если оно относится к курсу текущего преподавателя
                var homework = await _context.Homeworks
                    .Include(h => h.Student)
                    .Include(h => h.Lesson)
                        .ThenInclude(l => l.Course)
                    .Include(h => h.Files)
                    .FirstOrDefaultAsync(h => h.Id == homeworkId);


                if (homework == null)
                {
                    return NotFound("Сданная работа не найдено или у вас нет доступа.");
                }

                // ✅ К этому моменту Lesson и Course гарантированно загружены

                ViewBag.ReturnUrl = returnUrl ?? Url.Action(nameof(CourseDetails), new
                {
                    id = homework.Lesson.Course.Id,
                    status = "Pending"
                });

                return View(new ReviewHomeworkViewModel
                {
                    Homework = homework,
                    Feedback = homework.Feedback,
                    Status = homework.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке работы {homeworkId}");
                return StatusCode(500, "Произошла ошибка при загрузке работы");
            }
        }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ReviewHomework(int homeworkId, ReviewHomeworkViewModel model, string returnUrl = null)
            {
                if (!ModelState.IsValid)
                {
                var homework = await _context.Homeworks
                   .Include(h => h.Lesson)
                       .ThenInclude(l => l.Course)
                   .Include(h => h.Student)
                   .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null)
                        return NotFound();

                    model.Homework = homework;
                    ViewBag.ReturnUrl = returnUrl;


                    _logger.LogWarning("ModelState INVALID:");
                    foreach (var kvp in ModelState)
                    foreach (var err in kvp.Value.Errors)
                        _logger.LogWarning($" - {kvp.Key}: {err.ErrorMessage}");

                return View(model);
                }

            try
                {
                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                            .ThenInclude(l => l.Course)
                        .Include(h => h.Student)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);


                    if (homework == null || homework.Lesson.Course.TeacherId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    homework.Feedback = model.Feedback;
                    homework.Status = model.Status;
                    homework.SubmittedAt = DateTime.UtcNow;
                    _logger.LogInformation("Пытаюсь сохранить: Feedback={Feedback}, Status={Status}", model.Feedback, model.Status);
                    _logger.LogInformation("Сохранено");
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        homework.StudentId,
                        "Работа проверена",
                        $"Ваша работа по уроку \"{homework.Lesson.Title}\" была проверена",
                        NotificationType.HomeworkGraded
                    );

                    // Проверяем и выдаем сертификат, если все задания выполнены
                    if (model.Status == HomeworkStatus.Approved)
                    {
                        _logger.LogInformation($"Попытка выдачи сертификата для студента {homework.StudentId} по курсу {homework.Lesson.CourseId}");
                        var certificateIssued = await _certificateService.IssueCertificateIfEligibleAsync(homework.StudentId, homework.Lesson.CourseId);
                        _logger.LogInformation($"Результат выдачи сертификата: {certificateIssued}");
                    }

                    TempData["SuccessMessage"] = "Работа успешно проверена!";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("CourseDetails", new { id = homework.Lesson.Course.Id });

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при проверке работы {homeworkId}");
                    ModelState.AddModelError("", "Произошла ошибка при сохранении проверки");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }
            }

            [HttpGet]
            public async Task<IActionResult> AddLesson(int courseId)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);
                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

                    if (course == null)
                    {
                        _logger.LogWarning($"Попытка доступа к несуществующему курсу {courseId} или курсу другого преподавателя");
                        return NotFound();
                    }

                    var modules = await _context.Modules
                        .Where(m => m.CourseId == courseId)
                        .OrderBy(m => m.OrderNumber)
                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                        .ToListAsync();

                    return View(new AddLessonViewModel
                    {
                        CourseId = courseId,
                        Modules = modules
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при загрузке формы создания урока для курса {courseId}");
                    return StatusCode(500, "Произошла ошибка при загрузке формы");
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddLesson(AddLessonViewModel model)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        // подгружаем модули снова при ошибках
                        model.Modules = await _context.Modules
                            .Where(m => m.CourseId == model.CourseId)
                            .OrderBy(m => m.OrderNumber)
                            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                            .ToListAsync();
                        _logger.LogWarning("ModelState невалиден при создании урока");
                        foreach (var kvp in ModelState)
                        {
                            foreach (var err in kvp.Value.Errors)
                            {
                                _logger.LogWarning($" - {kvp.Key}: {err.ErrorMessage}");
                            }
                        }
                        return View(model);
                    }

                    var teacherId = _userManager.GetUserId(User);
                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Id == model.CourseId && c.TeacherId == teacherId);

                    if (course == null)
                    {
                        _logger.LogWarning($"Попытка создания урока для несуществующего курса {model.CourseId} или курса другого преподавателя");
                        return NotFound();
                    }

                    var lesson = new Lesson
                    {
                        Title = model.Title,
                        Order = model.Order,
                        Content = model.Content,
                        CourseId = model.CourseId,
                        ModuleId = model.ModuleId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Lessons.Add(lesson);
                    await _context.SaveChangesAsync();

                    // Сохраняем файлы
                    if (model.Attachments != null && model.Attachments.Any())
                    {
                        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lesson.Id.ToString());
                        Directory.CreateDirectory(uploadPath);

                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                        foreach (var file in model.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                _logger.LogInformation($"Загрузка файла: {file.FileName}, размер: {file.Length}, тип: {file.ContentType}");
                                var fileName = Path.GetFileName(file.FileName);
                                var filePath = Path.Combine(uploadPath, fileName);
                                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                                
                                if (fileExtension == ".gif" || file.ContentType.ToLowerInvariant() == "image/gif")
                                {
                                    ModelState.AddModelError("Attachments", "GIF-изображения не поддерживаются. Загрузите JPG или PNG.");
                                    return View(model);
                                }
                                else if (!allowedExtensions.Contains(fileExtension))
                                {
                                    ModelState.AddModelError("Attachments", $"Недопустимый формат файла: {fileName}");
                                    return View(model);
                                }
                                try
                                {
                                    using var stream = new FileStream(filePath, FileMode.Create);
                                    await file.CopyToAsync(stream);
                                    _logger.LogInformation($"Файл успешно сохранен: {filePath}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Ошибка при сохранении файла {fileName}");
                                    ModelState.AddModelError("Attachments", $"Ошибка при сохранении файла {fileName}: {ex.Message}");
                                }
                            }
                        }
                    }

                    _logger.LogInformation($"Урок успешно создан: {lesson.Title} (ID: {lesson.Id}) для курса {course.Title}");
                    TempData["SuccessMessage"] = "Урок успешно добавлен!";
                    return RedirectToAction("CourseDetails", new { id = model.CourseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при создании урока для курса {model.CourseId}");
                    ModelState.AddModelError("", "Произошла ошибка при создании урока");
                    return View(model);
                }
            }


        // Список студентов курса
        public async Task<IActionResult> CourseStudents(int courseId)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);
                    var courseExists = await _context.Courses
                        .AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);

                    if (!courseExists)
                    {
                        return NotFound();
                    }

                    var students = await _context.UserCourses
                        .Include(uc => uc.User)
                        .Where(uc => uc.CourseId == courseId)
                        .Select(uc => uc.User)
                        .ToListAsync();

                    return View(students);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при загрузке студентов курса {courseId}");
                    return StatusCode(500, "Произошла ошибка при загрузке студентов");
                }
            }

            [HttpGet]
            public IActionResult CreateCourse()
            {
                return View();
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
                {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var course = new Course
                    {
                        Title = model.Title,
                        Description = model.Description,
                        Category = model.Category,
                        DifficultyLevel = model.DifficultyLevel,
                        TeacherId = teacherId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Курс успешно создан!";
                    return RedirectToAction(nameof(CourseDetails), new { id = course.Id });
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при создании курса");
                    ModelState.AddModelError("", "Произошла ошибка при создании курса");
                    return View(model);
                }
            }

            [HttpGet]
            public async Task<IActionResult> EditCourse(int id)
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null || course.TeacherId != _userManager.GetUserId(User))
                {
                    return NotFound();
                }

                var model = new EditCourseViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Category = course.Category,
                    DifficultyLevel = course.DifficultyLevel
                };

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EditCourse(EditCourseViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var course = await _context.Courses.FindAsync(model.Id);
                if (course == null || course.TeacherId != _userManager.GetUserId(User))
                {
                    return NotFound();
                }

                course.Title = model.Title;
                course.Description = model.Description;
                course.Category = model.Category;
                course.DifficultyLevel = model.DifficultyLevel;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Курс успешно обновлен!";
                return RedirectToAction(nameof(CourseDetails), new { id = course.Id });
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteLesson(int id)
            {
                try
                {
                    var lesson = await _context.Lessons
                        .Include(l => l.Course)
                        .FirstOrDefaultAsync(l => l.Id == id);

                    if (lesson == null || lesson.Course.TeacherId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    // Если есть связанные медиафайлы — удалить их из файловой системы здесь
                    // (если реализовано хранение путей в БД)

                    _context.Lessons.Remove(lesson);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Урок успешно удалён!";
                    return RedirectToAction(nameof(CourseDetails), new { id = lesson.CourseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при удалении урока с id={id}");
                    TempData["ErrorMessage"] = "Произошла ошибка при удалении урока";
                    return RedirectToAction(nameof(Index));
                }
            }


            [HttpGet]
            public async Task<IActionResult> EditLesson(int id)
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lesson == null || lesson.Course.TeacherId != _userManager.GetUserId(User))
                    return NotFound();

                var model = new EditLessonViewModel
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    Order = lesson.Order,
                    Content = lesson.Content,
                    CourseId = lesson.CourseId,
                    ExistingFiles = GetLessonFiles(lesson.Id),
                    ModuleId = lesson.ModuleId
                };

                model.Modules = await _context.Modules
                    .Where(m => m.CourseId == lesson.CourseId)
                    .OrderBy(m => m.OrderNumber)
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                    .ToListAsync();

                return View(model);
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EditLesson(EditLessonViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    model.ExistingFiles = GetLessonFiles(model.Id);
                    model.Modules = await _context.Modules
                        .Where(m => m.CourseId == model.CourseId)
                        .OrderBy(m => m.OrderNumber)
                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                        .ToListAsync();
                    return View(model);
                }

                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == model.Id);

                if (lesson == null || lesson.Course.TeacherId != _userManager.GetUserId(User))
                    return NotFound();

                lesson.Title = model.Title;
                lesson.Order = model.Order;
                lesson.Content = model.Content;
                lesson.ModuleId = model.ModuleId;

                // Загрузка файлов
                if (model.Attachments != null && model.Attachments.Any())
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lesson.Id.ToString());

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                    foreach (var file in model.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            _logger.LogInformation($"Загрузка файла: {file.FileName}, размер: {file.Length}, тип: {file.ContentType}");
                            var fileName = Path.GetFileName(file.FileName);
                            var filePath = Path.Combine(uploadPath, fileName);
                            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                            
                            if (fileExtension == ".gif" || file.ContentType.ToLowerInvariant() == "image/gif")
                            {
                                ModelState.AddModelError("Attachments", "GIF-изображения не поддерживаются. Загрузите JPG или PNG.");
                                return View(model);
                            }
                            else if (!allowedExtensions.Contains(fileExtension))
                            {
                                ModelState.AddModelError("Attachments", $"Недопустимый формат файла: {fileName}");
                                return View(model);
                            }
                            try
                            {
                                using var stream = new FileStream(filePath, FileMode.Create);
                                await file.CopyToAsync(stream);
                                _logger.LogInformation($"Файл успешно сохранен: {filePath}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Ошибка при сохранении файла {file.FileName}");
                                ModelState.AddModelError("Attachments", $"Ошибка при сохранении файла {file.FileName}: {ex.Message}");
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Урок успешно обновлён!";
                return RedirectToAction(nameof(CourseDetails), new { id = lesson.CourseId });
            }

        [HttpGet]
        public async Task<IActionResult> AddModule(int courseId)
        {
            var teacherId = _userManager.GetUserId(User);
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);
            if (course == null)
            {
                return NotFound();
            }

            return View(new CreateModuleViewModel { CourseId = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule(CreateModuleViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == model.CourseId && c.TeacherId == teacherId);
            if (course == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var module = new Module
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                OrderNumber = model.OrderNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Модуль добавлен";
            return RedirectToAction(nameof(CourseDetails), new { id = model.CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            var module = await _context.Modules
                .Include(m => m.Course)
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == id && m.Course.TeacherId == teacherId);

            if (module == null)
                return NotFound();

            // Обнуляем ModuleId у уроков
            foreach (var lesson in module.Lessons)
            {
                lesson.ModuleId = null;
            }

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Модуль удалён";
            return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
        }

            [HttpGet]
            public IActionResult DeleteLessonFile(int lessonId, string fileName)
            {
                var teacherId = _userManager.GetUserId(User);

                var lesson = _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefault(l => l.Id == lessonId && l.Course.TeacherId == teacherId);

                if (lesson == null)
                    return NotFound();

                var path = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lessonId.ToString(), fileName);

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                return RedirectToAction(nameof(EditLesson), new { id = lessonId });
            }

            private List<LessonFileViewModel> GetLessonFiles(int lessonId)
            {
                var path = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lessonId.ToString());
                if (!Directory.Exists(path))
                    return new List<LessonFileViewModel>();

                return Directory.GetFiles(path)
                    .Select(f => new LessonFileViewModel
                    {
                        FileName = Path.GetFileName(f),
                        FilePath = "/uploads/lessons/" + lessonId + "/" + Path.GetFileName(f)
                    }).ToList();
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteCourse(int id)
            {
                var teacherId = _userManager.GetUserId(User);

                var course = await _context.Courses
                    .Include(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                    .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

                if (course == null)
                {
                    _logger.LogWarning($"Курс с id={id} не найден или доступ запрещён");
                    return NotFound();
                }

                try
                {
                    // 🧹 Удаление файлов каждого урока (если хранишь файлы в /uploads/lessons/{lessonId})
                    foreach (var lesson in course.Lessons)
                    {
                        var lessonDir = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lesson.Id.ToString());
                        if (Directory.Exists(lessonDir))
                        {
                            Directory.Delete(lessonDir, true); // рекурсивно
                        }
                    }

                    // Удаляем связанные сущности, которые могут блокировать удаление
                    var lessonIds = course.Lessons.Select(l => l.Id).ToList();
                    var homeworkIds = course.Lessons.SelectMany(l => l.Homeworks).Select(h => h.Id).ToList();

                    var homeworkFiles = _context.HomeworkFiles.Where(f => homeworkIds.Contains(f.HomeworkId));
                    var homeworks = _context.Homeworks.Where(h => homeworkIds.Contains(h.Id));
                    var lessonComments = _context.LessonComments.Where(c => lessonIds.Contains(c.LessonId));
                    var userCourses = _context.UserCourses.Where(uc => uc.CourseId == id);
                    var reviews = _context.Reviews.Where(r => r.CourseId == id);
                    var categories = _context.Categories.Where(cat => cat.CourseId == id);
                    var modules = _context.Modules.Where(m => m.CourseId == id);
                    var certificates = _context.Certificates.Where(c => c.CourseId == id);
                    var notifications = _context.Notifications
                        .Where(n => n.CourseId == id || (n.LessonId != null && lessonIds.Contains(n.LessonId.Value)) || (n.HomeworkId != null && homeworkIds.Contains(n.HomeworkId.Value)));
                    var lessons = _context.Lessons.Where(l => lessonIds.Contains(l.Id));

                    _context.HomeworkFiles.RemoveRange(homeworkFiles);
                    _context.Homeworks.RemoveRange(homeworks);
                    _context.LessonComments.RemoveRange(lessonComments);
                    _context.Notifications.RemoveRange(notifications);
                    _context.UserCourses.RemoveRange(userCourses);
                    _context.Reviews.RemoveRange(reviews);
                    _context.Categories.RemoveRange(categories);
                    _context.Modules.RemoveRange(modules);
                    _context.Certificates.RemoveRange(certificates);
                    _context.Lessons.RemoveRange(lessons);

                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Курс {course.Title} успешно удалён (id={id})");
                    TempData["SuccessMessage"] = $"Курс «{course.Title}» был удалён.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при удалении курса с id={id}");
                    TempData["ErrorMessage"] = "Ошибка при удалении курса";
                    return RedirectToAction(nameof(Index));
                }
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
                    return BadRequest("GIF-изображения не поддерживаются для аватаров. Загрузите JPG или PNG.");
                }
                else if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Недопустимый формат файла. Разрешены только .jpg, .jpeg, .png");
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

            [HttpGet]
            public async Task<IActionResult> DownloadFile(int homeworkId, int fileId)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);
                    
                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                            .ThenInclude(l => l.Course)
                        .Include(h => h.Files)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != teacherId)
                        return NotFound($"Homework not found or access denied. homeworkId={homeworkId}");

                    var file = homework.Files.FirstOrDefault(f => f.Id == fileId);
                    if (file == null)
                        return NotFound($"File not found in homework. fileId={fileId}");

                    var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/'));
                    
                    if (!System.IO.File.Exists(filePath))
                        return NotFound($"File not found on disk: {filePath}");

                    Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{file.FileName}\"");
                    var mimeType = GetMimeType(Path.GetExtension(file.FileName));
                    return PhysicalFile(filePath, mimeType, file.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при скачивании файла {fileId} из домашней работы {homeworkId}");
                    return StatusCode(500, "Произошла ошибка при скачивании файла");
                }
            }

            private string GetMimeType(string extension)
            {
                return extension.ToLower() switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".txt" => "text/plain",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".zip" => "application/zip",
                    ".rar" => "application/x-rar-compressed",
                    _ => "application/octet-stream"
                };
            }

    }
}