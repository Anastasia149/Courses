using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public class Module
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        public int OrderNumber { get; set; } // Порядковый номер

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Связи
        public Course Course { get; set; }
    }
}

