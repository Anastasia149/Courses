using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace Courses.ViewModels
{
    public class EditCourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        public string Title { get; set; }

        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        [Display(Name = "Description")]
        public string ShortDescription { get; set; }

        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }
        
        public List<SelectListItem> Categories { get; set; } = new();

        [Display(Name = "Уровень сложности")]
        [StringLength(20, ErrorMessage = "Не более 20 символов")]
        public string DifficultyLevel { get; set; }

        [Display(Name = "Course Language")]
        public string Language { get; set; } = "Русский";

        [Display(Name = "Course Cover")]
        public IFormFile? CoverImage { get; set; }

        public string? ExistingCoverImagePath { get; set; }

        public string? SelectedCategories { get; set; }
    }
}