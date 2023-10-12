using System.ComponentModel.DataAnnotations;

namespace AWSS3Manage.Models
{
    public class UserLoginViewModel
    {
        [Required(ErrorMessage = "Email can't be null !!!")]
        [EmailAddress(ErrorMessage = "Enter a real mail address !!!")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password can't be null !!!")]
        public string? Password { get; set; }
    }
}
