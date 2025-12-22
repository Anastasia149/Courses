using Courses.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class AddLessonViewModel
    {
        [Required(ErrorMessage = "Название урока обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название")]
        public string Title { get; set; }

        // Order вычисляется автоматически в контроллере
        public int Order { get; set; }

        [Required(ErrorMessage = "Содержание урока обязательно")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Содержание")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Тип урока обязателен")]
        [Display(Name = "Тип урока")]
        public LessonType Type { get; set; } = LessonType.Lecture;

        public int CourseId { get; set; }

        [Display(Name = "Дополнительные материалы")]
        public IFormFile[]? Attachments { get; set; }

        [Display(Name = "Модуль")]
        public int? ModuleId { get; set; }

        public IEnumerable<SelectListItem> Modules { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
