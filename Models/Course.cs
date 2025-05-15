using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Category { get; set; }
        public string? DifficultyLevel { get; set; }

        // Связи
        public string TeacherId { get; set; } // ID преподавателя (связь с User)
        public User Teacher { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
        public List<UserCourse> UserCourses { get; set; } = new();
    }
}