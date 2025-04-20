using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Courses.ViewModels
{
    public class TeacherProfileViewModel
    {
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }
        [Display(Name = "Current Avatar")]
        public string? ExistingAvatarPath { get; set; }

        [Display(Name = "Avatar Image")]
        public IFormFile? AvatarFile { get; set; }
    }
}