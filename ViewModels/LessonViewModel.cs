using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class LessonViewModel
    {
        [Required]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        [Range(1, 100)]
        public int Order { get; set; }

        public int CourseId { get; set; }
        public List<IFormFile> Attachments { get; set; } = new();
    }
}
