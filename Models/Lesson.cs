namespace Courses.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // HTML-контент
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Order { get; set; } // Порядковый номер в курсе

        // Связи
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public List<Homework> Homeworks { get; set; } = new();
    }
}