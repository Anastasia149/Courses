using System.ComponentModel.DataAnnotations;
using Courses.Models;
using System.Collections.Generic;

namespace Courses.ViewModels
{
    public class StudentCoursesViewModel
    {
        public List<StudentCourseViewModel> EnrolledCourses { get; set; } = new();
    }

    public class StudentCourseViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TeacherName { get; set; }
        public int LessonsCount { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int PendingHomeworksCount { get; set; }
    }

    public class StudentCourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TeacherName { get; set; }
        public List<StudentLessonViewModel> Lessons { get; set; } = new();
        public StudentLessonViewModel SelectedLesson { get; set; }
    }

    public class StudentLessonViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool HasHomework { get; set; }
        public HomeworkStatus HomeworkStatus { get; set; }
        public List<LessonFileViewModel> Files { get; set; } = new();
    }
} 