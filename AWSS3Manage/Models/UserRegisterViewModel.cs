using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace AWSS3Manage.Models
{
    public class UserRegisterViewModel
    {
        [Display(Name = "Username")]
        [Required(ErrorMessage = "Username can't be null !!!")]
        public string? Username { get; set; }

        public string? UserDescription { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email can't be null !!!")]
        [EmailAddress(ErrorMessage = "Enter a real mail adress !!!")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password can't be null !!!")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password Again")]
        [Compare("Password",ErrorMessage = "Password didn't match !!!")]
        public string? ConfirmPassword { get; set; }
    }
}
