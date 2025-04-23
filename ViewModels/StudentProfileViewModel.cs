using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class StudentProfileViewModel
    {
        [Display(Name = "Имя")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Номер телефона")]
        [Phone(ErrorMessage = "Некорректный номер телефона")]
        public string? PhoneNumber { get; set; }
        [Display(Name = "Текущий аватар")]
        public string? ExistingAvatarPath { get; set; }

        [Display(Name = "Изображение аватара")]
        public IFormFile? AvatarFile { get; set; }
    }
}
