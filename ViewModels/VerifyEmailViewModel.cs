using System.ComponentModel.DataAnnotations;

namespace Courses.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Требуется Email.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
