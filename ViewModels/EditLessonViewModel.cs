using Microsoft.AspNetCore.Http;
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

        [Required]
        public int Order { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public List<IFormFile> Attachments { get; set; } = new();

        public List<LessonFileViewModel> ExistingFiles { get; set; } = new();
    }

    public class LessonFileViewModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

}
