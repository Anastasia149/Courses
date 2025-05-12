using Courses.Models;

namespace Courses.ViewModels
{
    public class TeacherCoursesViewModel
    {
        public List<Course> Courses { get; set; }
        public int? SelectedCourseId { get; set; }
        public CourseDetailsViewModel SelectedCourse { get; set; }
    }
}
