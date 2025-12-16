using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Courses.ViewModels
{
    public class EditCourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }
        
        public List<SelectListItem> Categories { get; set; } = new();

        [Display(Name = "Уровень сложности")]
        [StringLength(20, ErrorMessage = "Не более 20 символов")]
        public string DifficultyLevel { get; set; }

    }
}