using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public class CourseCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}

