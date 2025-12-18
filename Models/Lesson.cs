using System.ComponentModel.DataAnnotations;

namespace Courses.Models
{
    public enum LessonType
    {
        [Display(Name = "Лекция")]
        Lecture = 0,
        
        [Display(Name = "Сообщение")]
        Message = 1,
        
        [Display(Name = "Задание")]
        Assignment = 2,
        
        [Display(Name = "Видео")]
        Video = 3
    }

    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // HTML-контент
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Order { get; set; } // Порядковый номер в курсе
        public LessonType Type { get; set; } = LessonType.Lecture; // Тип урока

        // Связи
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int? ModuleId { get; set; }
        public Module? Module { get; set; }
        public List<Homework> Homeworks { get; set; } = new();
    }
}