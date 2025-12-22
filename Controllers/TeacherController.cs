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
                    .Include(c => c.UserCourses)
                    .Include(c => c.Reviews)
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

            // Автоматически вычисляем порядковый номер модуля
            var maxOrderNumber = await _context.Modules
                .Where(m => m.CourseId == model.CourseId)
                .Select(m => (int?)m.OrderNumber)
                .MaxAsync() ?? 0;

            var module = new Module
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Description = null,
                OrderNumber = maxOrderNumber + 1
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

                // Получаем отзывы для курса
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.CourseId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                var model = new CourseDetailsViewModel
                {
                    Course = selectedCourse,
                    PendingHomeworks = homeworks,
                    EnrolledStudentsCount = enrolledUsers.Count,
                    EnrolledStudents = enrolledUsers.Select(uc => uc.User).ToList(),
                    CurrentStatus = status
                };

                // Передаем информацию о датах регистрации
                ViewBag.Enrollments = enrolledUsers.ToDictionary(uc => uc.UserId, uc => uc.EnrollmentDate);
                // Передаем отзывы для аналитики
                ViewBag.Reviews = reviews;
                ViewBag.AverageRating = averageRating;
                ViewBag.TotalReviews = reviews.Count;

                return View("~/Views/Teacher/CourseDetails.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке курса {id}");
                return StatusCode(500, "Произошла ошибка при загрузке курса");
            }
        }

        // Partial view endpoint for AJAX loading of course details (tabs only)
        [HttpGet]
        public async Task<IActionResult> CourseDetailsPartial(int id, HomeworkStatus? status = HomeworkStatus.Pending)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

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

                var homeworksQuery = selectedCourse.Lessons
                    .SelectMany(l => l.Homeworks);

                List<Homework> homeworks;
                if (status == null || status == HomeworkStatus.Pending)
                {
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

                var model = new CourseDetailsViewModel
                {
                    Course = selectedCourse,
                    PendingHomeworks = homeworks,
                    EnrolledStudentsCount = enrolledUsers.Count,
                    EnrolledStudents = enrolledUsers.Select(uc => uc.User).ToList(),
                    CurrentStatus = status
                };

                ViewBag.Enrollments = enrolledUsers.ToDictionary(uc => uc.UserId, uc => uc.EnrollmentDate);

                return PartialView("_CourseDetailsPartial", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке частичного представления курса {id}");
                return StatusCode(500, "Произошла ошибка при загрузке данных");
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
            public async Task<IActionResult> ReviewLessonAssignments(int lessonId, string? studentId = null)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var lesson = await _context.Lessons
                        .Include(l => l.Course)
                        .Include(l => l.Homeworks)
                            .ThenInclude(h => h.Student)
                        .Include(l => l.Homeworks)
                            .ThenInclude(h => h.Files)
                        .Include(l => l.Homeworks)
                            .ThenInclude(h => h.Comments)
                                .ThenInclude(c => c.User)
                        .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.TeacherId == teacherId);

                    if (lesson == null)
                    {
                        return NotFound();
                    }

                    // Получаем всех студентов курса
                    var enrolledStudents = await _context.UserCourses
                        .Include(uc => uc.User)
                        .Where(uc => uc.CourseId == lesson.CourseId)
                        .Select(uc => uc.User)
                        .ToListAsync();

                    // Получаем все задания для этого урока
                    var homeworks = await _context.Homeworks
                        .Include(h => h.Comments)
                        .Where(h => h.LessonId == lessonId && h.Status != HomeworkStatus.Cancelled)
                        .ToListAsync();

                    var studentsInfo = enrolledStudents.Select(student =>
                    {
                        var studentHomework = homeworks.FirstOrDefault(h => h.StudentId == student.Id);
                        var newCommentsCount = 0;
                        if (studentHomework != null)
                        {
                            // Подсчитываем новые комментарии (от студента после отправки задания)
                            newCommentsCount = studentHomework.Comments
                                .Count(c => c.UserId == student.Id && c.CreatedAt > studentHomework.SubmittedAt);
                        }
                        return new StudentAssignmentInfo
                        {
                            Student = student,
                            Homework = studentHomework,
                            NewCommentsCount = newCommentsCount
                        };
                    }).ToList();

                    Homework? selectedHomework = null;
                    if (!string.IsNullOrEmpty(studentId))
                    {
                        selectedHomework = await _context.Homeworks
                            .Include(h => h.Student)
                            .Include(h => h.Files)
                            .Include(h => h.Comments)
                                .ThenInclude(c => c.User)
                            .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == studentId);
                    }

                    var model = new ReviewLessonAssignmentsViewModel
                    {
                        Lesson = lesson,
                        Course = lesson.Course,
                        Students = studentsInfo,
                        SelectedHomework = selectedHomework,
                        SelectedStudentId = studentId
                    };

                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при загрузке заданий урока {lessonId}");
                    return StatusCode(500, "Произошла ошибка при загрузке данных");
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AcceptAssignment(int homeworkId)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                            .ThenInclude(l => l.Course)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != teacherId)
                    {
                        return NotFound();
                    }

                    homework.Status = HomeworkStatus.Approved;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        homework.StudentId,
                        "Задание принято",
                        $"Ваше задание по уроку \"{homework.Lesson.Title}\" было принято",
                        NotificationType.HomeworkGraded,
                        homeworkId
                    );

                    // Проверяем и выдаем сертификат
                    await _certificateService.IssueCertificateIfEligibleAsync(homework.StudentId, homework.Lesson.CourseId);

                    return RedirectToAction("ReviewLessonAssignments", new { lessonId = homework.LessonId, studentId = homework.StudentId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при принятии задания {homeworkId}");
                    return StatusCode(500, "Произошла ошибка при сохранении");
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> RejectAssignment(int homeworkId)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                            .ThenInclude(l => l.Course)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != teacherId)
                    {
                        return NotFound();
                    }

                    homework.Status = HomeworkStatus.Rejected;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        homework.StudentId,
                        "Требуется доработка",
                        $"Ваше задание по уроку \"{homework.Lesson.Title}\" требует доработки",
                        NotificationType.HomeworkGraded,
                        homeworkId
                    );

                    return RedirectToAction("ReviewLessonAssignments", new { lessonId = homework.LessonId, studentId = homework.StudentId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при отклонении задания {homeworkId}");
                    return StatusCode(500, "Произошла ошибка при сохранении");
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddHomeworkComment(int homeworkId, string text)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                            .ThenInclude(l => l.Course)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != teacherId)
                    {
                        return NotFound();
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return BadRequest("Комментарий не может быть пустым");
                    }

                    var comment = new HomeworkComment
                    {
                        HomeworkId = homeworkId,
                        UserId = teacherId,
                        Text = text,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.HomeworkComments.Add(comment);
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        homework.StudentId,
                        "Новый комментарий",
                        $"Преподаватель оставил комментарий к вашему заданию по уроку \"{homework.Lesson.Title}\"",
                        NotificationType.HomeworkGraded,
                        homeworkId
                    );

                    return RedirectToAction("ReviewLessonAssignments", new { lessonId = homework.LessonId, studentId = homework.StudentId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при добавлении комментария к заданию {homeworkId}");
                    return StatusCode(500, "Произошла ошибка при сохранении комментария");
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

                    // Автоматически вычисляем порядковый номер урока
                    var maxOrder = await _context.Lessons
                        .Where(l => l.CourseId == model.CourseId && (model.ModuleId == null ? l.ModuleId == null : l.ModuleId == model.ModuleId))
                        .Select(l => (int?)l.Order)
                        .MaxAsync() ?? 0;

                    // Сначала валидируем файлы, если они есть
                    if (model.Attachments != null && model.Attachments.Any())
                    {
                        // Определяем разрешенные расширения в зависимости от типа урока
                        string[] allowedExtensions;
                        if (model.Type == Models.LessonType.Video)
                        {
                            allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
                        }
                        else
                        {
                            allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                        }

                        foreach (var file in model.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Path.GetFileName(file.FileName);
                                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                                
                                if (model.Type != Models.LessonType.Video && (fileExtension == ".gif" || file.ContentType.ToLowerInvariant() == "image/gif"))
                                {
                                    ModelState.AddModelError("Attachments", "GIF-изображения не поддерживаются. Загрузите JPG или PNG.");
                                    model.Modules = await _context.Modules
                                        .Where(m => m.CourseId == model.CourseId)
                                        .OrderBy(m => m.OrderNumber)
                                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                                        .ToListAsync();
                                    return View(model);
                                }
                                else if (!allowedExtensions.Contains(fileExtension))
                                {
                                    var errorMsg = model.Type == Models.LessonType.Video 
                                        ? $"Для типа 'Видео' можно загружать только видео файлы (MP4, AVI, MOV, WMV, FLV, WEBM, MKV). Недопустимый формат: {fileName}"
                                        : $"Недопустимый формат файла: {fileName}";
                                    ModelState.AddModelError("Attachments", errorMsg);
                                    model.Modules = await _context.Modules
                                        .Where(m => m.CourseId == model.CourseId)
                                        .OrderBy(m => m.OrderNumber)
                                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                                        .ToListAsync();
                                    return View(model);
                                }
                            }
                        }
                    }

                    var lesson = new Lesson
                    {
                        Title = model.Title,
                        Order = maxOrder + 1,
                        Content = model.Content,
                        Type = model.Type,
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

                        // Определяем разрешенные расширения в зависимости от типа урока
                        string[] allowedExtensions;
                        if (lesson.Type == Models.LessonType.Video)
                        {
                            allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
                        }
                        else
                        {
                            allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                        }

                        foreach (var file in model.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                _logger.LogInformation($"Загрузка файла: {file.FileName}, размер: {file.Length}, тип: {file.ContentType}");
                                var fileName = Path.GetFileName(file.FileName);
                                var filePath = Path.Combine(uploadPath, fileName);
                                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
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
                                    
                                    // Если произошла ошибка при сохранении файла, удаляем урок
                                    _context.Lessons.Remove(lesson);
                                    await _context.SaveChangesAsync();
                                    
                                    model.Modules = await _context.Modules
                                        .Where(m => m.CourseId == model.CourseId)
                                        .OrderBy(m => m.OrderNumber)
                                        .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                                        .ToListAsync();
                                    return View(model);
                                }
                            }
                        }
                    }

                    // Проверяем, есть ли ошибки после сохранения файлов
                    if (!ModelState.IsValid)
                    {
                        // Если есть ошибки, удаляем урок
                        _context.Lessons.Remove(lesson);
                        await _context.SaveChangesAsync();
                        
                        model.Modules = await _context.Modules
                            .Where(m => m.CourseId == model.CourseId)
                            .OrderBy(m => m.OrderNumber)
                            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                            .ToListAsync();
                        return View(model);
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
            public async Task<IActionResult> CreateCourse()
            {
                var categories = await _context.CourseCategories
                    .OrderBy(c => c.Name)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync();

                var model = new CreateCourseViewModel
                {
                    Categories = categories
                };

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
                {
                _logger.LogInformation("CreateCourse POST called. Title: {Title}, ModelState.IsValid: {IsValid}", 
                    model?.Title, ModelState.IsValid);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                        string.Join(", ", ModelState.SelectMany(x => x.Value.Errors).Select(e => e.ErrorMessage)));
                    
                    var categories = await _context.CourseCategories
                        .OrderBy(c => c.Name)
                        .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToListAsync();
                    model.Categories = categories;
                    return View(model);
                }

                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    // Сохранение обложки курса
                    string coverImagePath = null;
                    if (model.CoverImage != null && model.CoverImage.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "course-covers");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var fileExtension = Path.GetExtension(model.CoverImage.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("CoverImage", "Разрешены форматы: JPG, JPEG, PNG");
                            var categories = await _context.CourseCategories
                                .OrderBy(c => c.Name)
                                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                                {
                                    Value = c.Id.ToString(),
                                    Text = c.Name
                                })
                                .ToListAsync();
                            model.Categories = categories;
                            return View(model);
                        }

                        if (model.CoverImage.Length > 10 * 1024 * 1024)
                        {
                            ModelState.AddModelError("CoverImage", "Размер файла не должен превышать 10MB");
                            var categories = await _context.CourseCategories
                                .OrderBy(c => c.Name)
                                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                                {
                                    Value = c.Id.ToString(),
                                    Text = c.Name
                                })
                                .ToListAsync();
                            model.Categories = categories;
                            return View(model);
                        }

                        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.CoverImage.CopyToAsync(stream);
                        }
                        coverImagePath = $"/uploads/course-covers/{uniqueFileName}";
                    }

                    var course = new Course
                    {
                        Title = model.Title,
                        Description = model.ShortDescription,
                        CategoryId = model.CategoryId,
                        DifficultyLevel = model.DifficultyLevel,
                        CoverImagePath = coverImagePath,
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
                    
                    // Загружаем категории обратно при ошибке
                    var categories = await _context.CourseCategories
                        .OrderBy(c => c.Name)
                        .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToListAsync();
                    model.Categories = categories;
                    
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

                var categories = await _context.CourseCategories
                    .OrderBy(c => c.Name)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == course.CategoryId
                    })
                    .ToListAsync();

                var model = new EditCourseViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    ShortDescription = course.Description,
                    CategoryId = course.CategoryId,
                    Categories = categories,
                    DifficultyLevel = course.DifficultyLevel,
                    ExistingCoverImagePath = course.CoverImagePath,
                    Language = "Русский" // Можно добавить поле Language в модель Course позже
                };

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EditCourse(EditCourseViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    var categories = await _context.CourseCategories
                        .OrderBy(c => c.Name)
                        .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToListAsync();
                    model.Categories = categories;
                    model.ExistingCoverImagePath = (await _context.Courses.FindAsync(model.Id))?.CoverImagePath;
                    return View(model);
                }

                var course = await _context.Courses.FindAsync(model.Id);
                if (course == null || course.TeacherId != _userManager.GetUserId(User))
                {
                    return NotFound();
                }

                // Сохранение новой обложки курса
                string coverImagePath = course.CoverImagePath; // Сохраняем существующую
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "course-covers");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(model.CoverImage.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("CoverImage", "Разрешены форматы: JPG, JPEG, PNG");
                        var categories = await _context.CourseCategories
                            .OrderBy(c => c.Name)
                            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                            {
                                Value = c.Id.ToString(),
                                Text = c.Name
                            })
                            .ToListAsync();
                        model.Categories = categories;
                        model.ExistingCoverImagePath = course.CoverImagePath;
                        return View(model);
                    }

                    if (model.CoverImage.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("CoverImage", "Размер файла не должен превышать 10MB");
                        var categories = await _context.CourseCategories
                            .OrderBy(c => c.Name)
                            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                            {
                                Value = c.Id.ToString(),
                                Text = c.Name
                            })
                            .ToListAsync();
                        model.Categories = categories;
                        model.ExistingCoverImagePath = course.CoverImagePath;
                        return View(model);
                    }

                    // Удаляем старую обложку, если была
                    if (!string.IsNullOrEmpty(course.CoverImagePath))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, course.CoverImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(stream);
                    }
                    coverImagePath = $"/uploads/course-covers/{uniqueFileName}";
                }

                course.Title = model.Title;
                course.Description = model.ShortDescription;
                course.CategoryId = model.CategoryId;
                course.DifficultyLevel = model.DifficultyLevel;
                course.CoverImagePath = coverImagePath;

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
                    Type = lesson.Type,
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
                // Order не изменяется при редактировании
                lesson.Content = model.Content;
                lesson.Type = model.Type;
                lesson.ModuleId = model.ModuleId;

                // Загрузка файлов
                if (model.Attachments != null && model.Attachments.Any())
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "lessons", lesson.Id.ToString());

                    // Определяем разрешенные расширения в зависимости от типа урока
                    string[] allowedExtensions;
                    if (lesson.Type == Models.LessonType.Video)
                    {
                        allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
                    }
                    else
                    {
                        allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                    }

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    foreach (var file in model.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            _logger.LogInformation($"Загрузка файла: {file.FileName}, размер: {file.Length}, тип: {file.ContentType}");
                            var fileName = Path.GetFileName(file.FileName);
                            var filePath = Path.Combine(uploadPath, fileName);
                            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                            
                            if (lesson.Type != Models.LessonType.Video && (fileExtension == ".gif" || file.ContentType.ToLowerInvariant() == "image/gif"))
                            {
                                ModelState.AddModelError("Attachments", "GIF-изображения не поддерживаются. Загрузите JPG или PNG.");
                                model.ExistingFiles = GetLessonFiles(model.Id);
                                model.Modules = await _context.Modules
                                    .Where(m => m.CourseId == model.CourseId)
                                    .OrderBy(m => m.OrderNumber)
                                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                                    .ToListAsync();
                                return View(model);
                            }
                            else if (!allowedExtensions.Contains(fileExtension))
                            {
                                var errorMsg = lesson.Type == Models.LessonType.Video 
                                    ? $"Для типа 'Видео' можно загружать только видео файлы (MP4, AVI, MOV, WMV, FLV, WEBM, MKV). Недопустимый формат: {fileName}"
                                    : $"Недопустимый формат файла: {fileName}";
                                ModelState.AddModelError("Attachments", errorMsg);
                                model.ExistingFiles = GetLessonFiles(model.Id);
                                model.Modules = await _context.Modules
                                    .Where(m => m.CourseId == model.CourseId)
                                    .OrderBy(m => m.OrderNumber)
                                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Title })
                                    .ToListAsync();
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

            // Автоматически вычисляем порядковый номер модуля
            var maxOrderNumber = await _context.Modules
                .Where(m => m.CourseId == model.CourseId)
                .Select(m => (int?)m.OrderNumber)
                .MaxAsync() ?? 0;

            var module = new Module
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Description = null,
                OrderNumber = maxOrderNumber + 1,
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
                    var homeworkComments = _context.HomeworkComments.Where(c => homeworkIds.Contains(c.HomeworkId));
                    var userCourses = _context.UserCourses.Where(uc => uc.CourseId == id);
                    var reviews = _context.Reviews.Where(r => r.CourseId == id);
                    var modules = _context.Modules.Where(m => m.CourseId == id);
                    var certificates = _context.Certificates.Where(c => c.CourseId == id);
                    var notifications = _context.Notifications
                        .Where(n => n.CourseId == id || (n.LessonId != null && lessonIds.Contains(n.LessonId.Value)) || (n.HomeworkId != null && homeworkIds.Contains(n.HomeworkId.Value)));
                    var lessons = _context.Lessons.Where(l => lessonIds.Contains(l.Id));

                    _context.HomeworkFiles.RemoveRange(homeworkFiles);
                    _context.HomeworkComments.RemoveRange(homeworkComments);
                    _context.Homeworks.RemoveRange(homeworks);
                    _context.Notifications.RemoveRange(notifications);
                    _context.UserCourses.RemoveRange(userCourses);
                    _context.Reviews.RemoveRange(reviews);
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