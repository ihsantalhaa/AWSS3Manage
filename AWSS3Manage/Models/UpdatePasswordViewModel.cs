using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace AWSS3Manage.Models
{
    public class UpdatePasswordViewModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        [Required(ErrorMessage = "Password can't be null !!!")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$", ErrorMessage = "The {0} must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        [Display(Name = "New Password")]
        [Required(ErrorMessage = "Password can't be null !!!")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "password didn't match !!!")]
        public string? ConfirmNewPassword { get; set; }
    }
}
