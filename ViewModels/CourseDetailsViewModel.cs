using Courses.Models;

namespace Courses.ViewModels
{
    public class CourseDetailsViewModel
    {
        public Course Course { get; set; }
        public int EnrolledStudentsCount { get; set; }
        public int PendingHomeworksCount { get; set; }
        public int CompletionRate { get; set; }
        public List<Homework> PendingHomeworks { get; set; }
        public List<User> EnrolledStudents { get; set; }

        // Добавим свойство для текущего статуса фильтрации
        public HomeworkStatus? CurrentStatus { get; set; }
    }
}
    