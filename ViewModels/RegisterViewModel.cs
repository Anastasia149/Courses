using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class RegisterViewModel
    {
        [Display(Name = "Имя")]
        [Required(ErrorMessage = "Требуется имя.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Требуется Email.")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Пароль")]
        [Required(ErrorMessage = "Требуется пароль.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} должен быть длиной {2} и иметь максимальную длину {1} ​​символов.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Пароль не совпадает.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Требуется подтверждения пароля.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} должен быть длиной {2} и иметь максимальную длину {1} ​​символов.")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Требуется роль.")]
        [Display(Name = "Роль")]
        public string Role { get; set; } // "Student" или "Teacher"

        [Display(Name = "Код")]
        public string? TeacherCode { get; set; } // Не обязательное поле, только для преподавателей
    }
}
