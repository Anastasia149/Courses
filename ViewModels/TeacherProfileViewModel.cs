using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Courses.ViewModels
{
    public class TeacherProfileViewModel
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