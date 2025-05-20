using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public enum HomeworkStatus { Pending, Approved, Rejected, Cancelled }

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

        // Коллекция файлов домашнего задания
        public ICollection<HomeworkFile> Files { get; set; } = new List<HomeworkFile>();
    }

    public class HomeworkFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public string ContentType { get; set; }

        [Required]
        public long FileSize { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int HomeworkId { get; set; }
        public Homework Homework { get; set; }
    }
}