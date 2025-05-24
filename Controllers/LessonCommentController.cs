using Courses.Data;
using Courses.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Courses.Controllers
{
    [Authorize]
    public class LessonCommentController : Controller
    {
        private readonly AppDbContext _context;

        public LessonCommentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isTeacher = User.IsInRole("Teacher");
            var comments = await _context.LessonComments
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Where(c => c.LessonId == lessonId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var result = comments.Select(c => new
            {
                c.Id,
                c.Text,
                c.CreatedAt,
                c.UserId,
                UserName = c.User.UserName,
                Replies = c.Replies.OrderBy(r => r.CreatedAt).Select(r => new
                {
                    r.Id,
                    r.Text,
                    r.CreatedAt,
                    r.UserId,
                    UserName = r.User.UserName
                })
            });

            return Json(new { comments = result, currentUserId = userId, isTeacher });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int lessonId, string text, int? parentCommentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest();

            if (string.IsNullOrEmpty(userId))
                return Forbid("UserId is null!");

            if (text != null && text.Length > 600)
                return BadRequest("Комментарий не должен превышать 600 символов.");

            var comment = new LessonComment
            {
                LessonId = lessonId,
                UserId = userId,
                Text = text,
                ParentCommentId = parentCommentId
            };
            _context.LessonComments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isTeacher = User.IsInRole("Teacher");
            var comment = await _context.LessonComments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
                return NotFound();
            if (comment.UserId != userId && !isTeacher)
                return Forbid();
            // Удаляем все дочерние комментарии
            if (comment.Replies != null && comment.Replies.Any())
            {
                _context.LessonComments.RemoveRange(comment.Replies);
            }
            _context.LessonComments.Remove(comment);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
} 