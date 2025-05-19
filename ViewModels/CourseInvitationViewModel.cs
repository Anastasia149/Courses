using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CourseInvitationViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Введите email студента")]
        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        public string StudentEmail { get; set; }
    }
} 