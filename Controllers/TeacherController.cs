    using Courses.Data;
    using Courses.Models;
    using Courses.ViewModels;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace Courses.Controllers
    {
        [Authorize(Roles = "Teacher")]
        public class TeacherController : Controller
        {
            private readonly AppDbContext _context;
            private readonly UserManager<User> _userManager;
            private readonly ILogger<TeacherController> _logger;

            public TeacherController(
                AppDbContext context,
                UserManager<User> userManager,
                ILogger<TeacherController> logger)
            {
                _context = context;
                _userManager = userManager;
                _logger = logger;
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

                    return View(courses);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при загрузке курсов преподавателя");
                    return StatusCode(500, "Произошла ошибка при загрузке данных");
                }
            }

            // Детали курса с фильтрацией ДЗ (без пагинации)
            public async Task<IActionResult> CourseDetails(int id, HomeworkStatus? status = HomeworkStatus.Pending)
            {
                try
                {
                    var teacherId = _userManager.GetUserId(User);

                    var course = await _context.Courses
                        .Include(c => c.Lessons)
                        .ThenInclude(l => l.Homeworks)
                        .ThenInclude(h => h.Student)
                        .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

                    if (course == null)
                    {
                        return NotFound();
                    }

                    var homeworks = course.Lessons
                        .SelectMany(l => l.Homeworks)
                        .Where(h => status == null || h.Status == status)
                        .ToList();

                    var model = new CourseDetailsViewModel
                    {
                        Course = course,
                        PendingHomeworks = homeworks,
                        EnrolledStudentsCount = await _context.UserCourses
                            .CountAsync(uc => uc.CourseId == id)
                    };

                    ViewBag.CurrentStatus = status;
                    return View(model);
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
                    var homework = await _context.Homeworks
                        .Include(h => h.Student)
                        .Include(h => h.Lesson)
                        .ThenInclude(l => l.Course)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    ViewBag.ReturnUrl = returnUrl ?? Url.Action(nameof(CourseDetails), new { id = homework.Lesson.CourseId });

                    return View(new ReviewHomeworkViewModel
                    {
                        Homework = homework,
                        Feedback = homework.Feedback,
                        Status = homework.Status
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при загрузке ДЗ {homeworkId}");
                    return StatusCode(500, "Произошла ошибка при загрузке работы");
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ReviewHomework(int homeworkId, ReviewHomeworkViewModel model, string returnUrl = null)
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }

                try
                {
                    var homework = await _context.Homeworks
                        .Include(h => h.Lesson)
                        .FirstOrDefaultAsync(h => h.Id == homeworkId);

                    if (homework == null || homework.Lesson.Course.TeacherId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    homework.Feedback = model.Feedback;
                    homework.Status = model.Status;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Работа успешно проверена!";
                    return Redirect(returnUrl ?? Url.Action(nameof(CourseDetails), new { id = homework.Lesson.CourseId }));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при проверке ДЗ {homeworkId}");
                    ModelState.AddModelError("", "Произошла ошибка при сохранении проверки");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }
            }

            // Добавление урока (GET)
            public IActionResult AddLesson(int courseId)
            {
                ViewBag.CourseId = courseId;
                return View(new LessonViewModel());
            }

            // Добавление урока (POST)
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddLesson(int courseId, LessonViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.CourseId = courseId;
                    return View(model);
                }

                try
                {
                    var course = await _context.Courses.FindAsync(courseId);
                    if (course == null || course.TeacherId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    var lesson = new Lesson
                    {
                        Title = model.Title,
                        Content = model.Content,
                        Order = model.Order,
                        CourseId = courseId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Lessons.Add(lesson);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Урок успешно добавлен!";
                    return RedirectToAction(nameof(CourseDetails), new { id = courseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при добавлении урока в курс {courseId}");
                    ModelState.AddModelError("", "Произошла ошибка при добавлении урока");
                    ViewBag.CourseId = courseId;
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

                    // Формируем полное описание из разных полей
                    var fullDescription = $"{model.ShortDescription}\n\n{model.Description}";

                    if (!string.IsNullOrWhiteSpace(model.Category))
                    {
                        fullDescription += $"\n\nКатегория: {model.Category}";
                    }

                    if (!string.IsNullOrWhiteSpace(model.DifficultyLevel))
                    {
                        fullDescription += $"\nУровень сложности: {model.DifficultyLevel}";
                    }

                    var course = new Course
                    {
                        Title = model.Title,
                        Description = fullDescription,
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
        }
    }