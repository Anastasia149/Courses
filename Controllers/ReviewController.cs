using Courses.Data;
using Courses.Models;
using Courses.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Courses.Controllers
{
    [Authorize(Roles = "Student")]
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReviewController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int courseId)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Проверяем, записан ли студент на курс
            var isEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId && uc.CourseId == courseId);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "Вы должны быть записаны на курс, чтобы просматривать отзывы.";
                return RedirectToAction("Course", "Student");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound();
            }

            // Получаем все отзывы для курса
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.CourseId == courseId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var userReview = reviews.FirstOrDefault(r => r.UserId == userId);
            var otherReviews = reviews.Where(r => r.UserId != userId).ToList();

            // Устанавливаем данные пользователя для layout
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;

            var model = new CourseReviewsViewModel
            {
                CourseId = courseId,
                CourseTitle = course.Title,
                Reviews = reviews,
                AverageRating = averageRating,
                TotalReviews = reviews.Count,
                HasUserReview = userReview != null,
                UserReview = userReview
            };

            ViewBag.OtherReviews = otherReviews;
            ViewBag.Course = course;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int courseId)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Проверяем, записан ли студент на курс
            var isEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId && uc.CourseId == courseId);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "Вы должны быть записаны на курс, чтобы оставить отзыв.";
                return RedirectToAction("CourseDetails", "Student", new { id = courseId });
            }

            // Проверяем, есть ли уже отзыв от этого пользователя
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);

            if (existingReview != null)
            {
                return RedirectToAction("Edit", new { id = existingReview.Id });
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var model = new CreateReviewViewModel
            {
                CourseId = courseId,
                CourseTitle = course.Title
            };

            // Устанавливаем данные пользователя для layout
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Проверяем, записан ли студент на курс
            var isEnrolled = await _context.UserCourses
                .AnyAsync(uc => uc.UserId == userId && uc.CourseId == model.CourseId);

            if (!isEnrolled)
            {
                ModelState.AddModelError("", "Вы должны быть записаны на курс, чтобы оставить отзыв.");
                return View(model);
            }

            // Проверяем, нет ли уже отзыва от этого пользователя
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == model.CourseId);

            if (existingReview != null)
            {
                ModelState.AddModelError("", "Вы уже оставили отзыв на этот курс. Вы можете отредактировать его.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var review = new Review
                {
                    UserId = userId,
                    CourseId = model.CourseId,
                    Rating = model.Rating,
                    Description = model.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Отзыв успешно добавлен!";
                return RedirectToAction("Index", "Review", new { courseId = model.CourseId });
            }

            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course != null)
            {
                model.CourseTitle = course.Title;
            }

            // Устанавливаем данные пользователя для layout
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            var model = new CreateReviewViewModel
            {
                CourseId = review.CourseId,
                CourseTitle = review.Course.Title,
                Rating = review.Rating,
                Description = review.Description
            };

            ViewBag.ReviewId = review.Id;
            
            // Устанавливаем данные пользователя для layout
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateReviewViewModel model)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                review.Rating = model.Rating;
                review.Description = model.Description;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Отзыв успешно обновлен!";
                return RedirectToAction("Index", "Review", new { courseId = review.CourseId });
            }

            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course != null)
            {
                model.CourseTitle = course.Title;
            }

            ViewBag.ReviewId = id;
            
            // Устанавливаем данные пользователя для layout
            var user = await _userManager.GetUserAsync(User);
            ViewBag.User = user;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            var courseId = review.CourseId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Отзыв успешно удален!";
            return RedirectToAction("Index", "Review", new { courseId = courseId });
        }
    }
}

