using Courses.Models;

namespace Courses.ViewModels
{
    public class ReviewLessonAssignmentsViewModel
    {
        public Lesson Lesson { get; set; }
        public Course Course { get; set; }
        public List<StudentAssignmentInfo> Students { get; set; } = new();
        public Homework? SelectedHomework { get; set; }
        public string? SelectedStudentId { get; set; }
    }

    public class StudentAssignmentInfo
    {
        public User Student { get; set; }
        public Homework? Homework { get; set; }
        public bool HasSubmitted => Homework != null && Homework.Status != HomeworkStatus.Cancelled;
        public int NewCommentsCount { get; set; }
    }
}

