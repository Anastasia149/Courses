using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CreateModuleViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Порядок")]
        [Range(1, 1000, ErrorMessage = "Порядок должен быть от 1 до 1000")]
        public int OrderNumber { get; set; } = 1;
    }
}

