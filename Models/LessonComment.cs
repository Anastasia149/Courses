using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public class LessonComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Для поддержки ответов на комментарии
        public int? ParentCommentId { get; set; }
        public LessonComment ParentComment { get; set; }
        public ICollection<LessonComment> Replies { get; set; } = new List<LessonComment>();
    }
} 