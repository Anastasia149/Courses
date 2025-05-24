using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Требуется Email.")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Пароль")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} должен быть длиной {2} и иметь максимальную длину {1} ​​символов.")]
        [Required(ErrorMessage = "Требуется пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня?")]
        public bool RememberMe { get; set; }
    }
}
