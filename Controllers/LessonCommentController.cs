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
                UserName = c.User.UserName,
                Replies = c.Replies.OrderBy(r => r.CreatedAt).Select(r => new
                {
                    r.Id,
                    r.Text,
                    r.CreatedAt,
                    UserName = r.User.UserName
                })
            });

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int lessonId, string text, int? parentCommentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest();

            if (string.IsNullOrEmpty(userId))
                return Forbid("UserId is null!");

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
    }
} 