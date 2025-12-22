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
        public int ProgressPercentage { get; set; }
        public string? CoverImagePath { get; set; }
    }

    public class StudentCourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TeacherName { get; set; }
        public string? CoverImagePath { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int ProgressPercentage { get; set; }
        public List<StudentModuleViewModel> Modules { get; set; } = new();
        public List<StudentLessonViewModel> Lessons { get; set; } = new();
        public StudentLessonViewModel? SelectedLesson { get; set; }
    }

    public class StudentModuleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int OrderNumber { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public List<StudentLessonViewModel> Lessons { get; set; } = new();
    }

    public class StudentLessonViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public int? ModuleId { get; set; }
        public bool HasHomework { get; set; }
        public HomeworkStatus HomeworkStatus { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsViewed { get; set; }
        public DateTime? DueDate { get; set; }
        public List<LessonFileViewModel> Files { get; set; } = new();
        public LessonType LessonType { get; set; }
    }
} 