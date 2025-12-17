using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        [Display(Name = "Course Title")]
        public string Title { get; set; }

        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        [Display(Name = "Description")]
        public string ShortDescription { get; set; }

        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }
        
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories { get; set; } = new();

        [Display(Name = "Уровень сложности")]
        [StringLength(20, ErrorMessage = "Не более 20 символов")]
        public string DifficultyLevel { get; set; }

        [Display(Name = "Course Language")]
        public string Language { get; set; } = "Русский";

        [Display(Name = "Course Cover")]
        public IFormFile? CoverImage { get; set; }

        public string? SelectedCategories { get; set; }
    }
}