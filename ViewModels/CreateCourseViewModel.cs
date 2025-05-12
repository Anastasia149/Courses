using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Не более 100 символов")]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Категория")]
        [StringLength(50, ErrorMessage = "Не более 50 символов")]
        public string Category { get; set; }

        [Display(Name = "Уровень сложности")]
        [StringLength(20, ErrorMessage = "Не более 20 символов")]
        public string DifficultyLevel { get; set; }

        [Display(Name = "Краткое описание")]
        [StringLength(200, ErrorMessage = "Не более 200 символов")]
        public string ShortDescription { get; set; }
    }
}