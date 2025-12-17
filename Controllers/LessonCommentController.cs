using Courses.Data;
using Courses.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Courses.Controllers
{
    [Authorize]
    public class LessonCommentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public LessonCommentController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int lessonId)
        {
            var userId = _userManager.GetUserId(User);
            var isTeacher = User.IsInRole("Teacher");
            
            // Получаем homeworkId для текущего студента и урока
            var homework = await _context.Homeworks
                .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId && h.Status != HomeworkStatus.Cancelled);
            
            if (homework == null)
            {
                return Json(new { comments = new object[0], currentUserId = userId, isTeacher });
            }

            var comments = await _context.HomeworkComments
                .Include(c => c.User)
                .Where(c => c.HomeworkId == homework.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var result = comments.Select(c => new
            {
                c.Id,
                c.Text,
                c.CreatedAt,
                c.UserId,
                UserName = c.User.FullName ?? c.User.UserName
            });

            return Json(new { comments = result, currentUserId = userId, isTeacher });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int lessonId, string text)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest();

            if (string.IsNullOrEmpty(userId))
                return Forbid("UserId is null!");

            if (text.Length > 1000)
                return BadRequest("Комментарий не должен превышать 1000 символов.");

            // Получаем или создаем homework для студента
            var homework = await _context.Homeworks
                .FirstOrDefaultAsync(h => h.LessonId == lessonId && h.StudentId == userId && h.Status != HomeworkStatus.Cancelled);

            if (homework == null)
            {
                return BadRequest("Задание не найдено. Сначала отправьте задание.");
            }

            var comment = new HomeworkComment
            {
                HomeworkId = homework.Id,
                UserId = userId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.HomeworkComments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(int commentId, string returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            var isTeacher = User.IsInRole("Teacher");
            
            var comment = await _context.HomeworkComments
                .Include(c => c.Homework)
                    .ThenInclude(h => h.Lesson)
                        .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(c => c.Id == commentId);
                
            if (comment == null)
                return NotFound();
            
            // Проверяем права:
            // 1. Автор комментария может удалить свой комментарий
            // 2. Преподаватель курса может удалить любой комментарий к заданию своего курса
            var isAuthor = comment.UserId == userId;
            var isCourseTeacher = isTeacher && comment.Homework.Lesson.Course.TeacherId == userId;
            
            if (!isAuthor && !isCourseTeacher)
            {
                return Forbid();
            }
            
            var homeworkId = comment.HomeworkId;
            var lessonId = comment.Homework.LessonId;
            var studentId = comment.Homework.StudentId;
            
            _context.HomeworkComments.Remove(comment);
            await _context.SaveChangesAsync();
            
            // Если это преподаватель, редиректим на страницу просмотра заданий
            if (isTeacher && !string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else if (isTeacher)
            {
                return RedirectToAction("ReviewLessonAssignments", "Teacher", new { lessonId = lessonId, studentId = studentId });
            }
            
            // Для студентов редиректим обратно на страницу урока
            return RedirectToAction("CourseDetails", "Student", new { id = comment.Homework.Lesson.CourseId, lessonId = lessonId });
        }
    }
} 