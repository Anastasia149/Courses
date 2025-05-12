using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public enum HomeworkStatus { Pending, Approved, Rejected }

    public class Homework
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Answer { get; set; }

        public string? Feedback { get; set; }
        public HomeworkStatus Status { get; set; } = HomeworkStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Lesson")]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }

        [ForeignKey("User")]
        public string StudentId { get; set; }
        public User Student { get; set; }
    }
}