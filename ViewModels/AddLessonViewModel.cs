using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class AddLessonViewModel
    {
        [Required]
        public string Title { get; set; }

        [Range(1, 100)]
        public int Order { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public int CourseId { get; set; }

        public IFormFile[] Attachments { get; set; }
    }
}
