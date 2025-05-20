using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public enum NotificationType
    {
        CourseInvitation,
        HomeworkSubmitted,
        HomeworkGraded,
        HomeworkReviewed,
        NewLesson
    }

    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Опциональные ссылки на связанные сущности
        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public int? LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        public int? HomeworkId { get; set; }
        public Homework? Homework { get; set; }
    }
} 