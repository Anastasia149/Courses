using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Требуется Email.")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Пароль")]
        [Required(ErrorMessage = "Требуется пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня?")]
        public bool RememberMe { get; set; }
    }
}
