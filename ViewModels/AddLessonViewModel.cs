using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class AddLessonViewModel
    {
        [Required(ErrorMessage = "Название урока обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Порядок урока обязателен")]
        [Range(1, 100, ErrorMessage = "Порядок должен быть от 1 до 100")]
        [Display(Name = "Порядок")]
        public int Order { get; set; }

        [Required(ErrorMessage = "Содержание урока обязательно")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Содержание")]
        public string Content { get; set; }

        public int CourseId { get; set; }

        [Display(Name = "Дополнительные материалы")]
        public IFormFile[]? Attachments { get; set; }
    }
}
