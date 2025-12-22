using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class CreateReviewViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }

        [Required(ErrorMessage = "Оценка обязательна")]
        [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
        [Display(Name = "Оценка")]
        public int Rating { get; set; }

        [Display(Name = "Отзыв")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Отзыв не должен превышать 1000 символов")]
        public string? Description { get; set; }
    }
}

