using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
        public int Rating { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Связи
        public User User { get; set; }
        public Course Course { get; set; }
    }
}

