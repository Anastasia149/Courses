using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CreateModuleViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        public string Title { get; set; }

        // Description и OrderNumber больше не используются
        // OrderNumber вычисляется автоматически в контроллере
        public int OrderNumber { get; set; }
    }
}

