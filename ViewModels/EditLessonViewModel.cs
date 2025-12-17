using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class EditLessonViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        [Required]
        public string Title { get; set; }

        // Order не изменяется при редактировании
        public int Order { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public List<IFormFile> Attachments { get; set; } = new();

        public List<LessonFileViewModel> ExistingFiles { get; set; } = new();

        [Display(Name = "Модуль")]
        public int? ModuleId { get; set; }

        public IEnumerable<SelectListItem> Modules { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
