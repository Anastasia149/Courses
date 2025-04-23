using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Требуется Email.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Требуется пароль.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} должен быть длиной {2} и иметь максимальную длину {1} ​​символов.")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        [Compare("ConfirmNewPassword", ErrorMessage = "Пароль не совпадает")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Требуется подтверждение пароля.")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение нового пароля")]
        public string ConfirmNewPassword { get; set; }
    }
}
