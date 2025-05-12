using Microsoft.AspNetCore.Identity;

namespace Courses.Models
{
    public class User:IdentityUser
    {
        public string FullName { get; set; }
        public string? AvatarPath { get; set; }

        public List<UserCourse> UserCourses { get; set; } = new();
        public List<Homework> Homeworks { get; set; } = new();
        public List<Course> TaughtCourses { get; set; } = new(); // Для преподавателя
    }
}
