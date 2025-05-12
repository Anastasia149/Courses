using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public class UserCourse
    {
        [Key] // Добавляем атрибут Key для явного указания первичного ключа
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Добавляем автоинкрементный идентификатор

        [ForeignKey("User")]
        [Required]
        public string UserId { get; set; }

        public User User { get; set; }

        [ForeignKey("Course")]
        [Required]
        public int CourseId { get; set; }

        public Course Course { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow; // Дополнительное поле
    }
}