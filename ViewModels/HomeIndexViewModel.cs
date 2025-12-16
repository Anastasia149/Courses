using Courses.Models;

namespace Courses.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Course> PopularCourses { get; set; } = new();
    }
}

