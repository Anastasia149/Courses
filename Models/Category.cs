using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        // Связи
        public Course Course { get; set; }
    }
}

