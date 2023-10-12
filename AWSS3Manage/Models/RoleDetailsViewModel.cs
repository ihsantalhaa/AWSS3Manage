using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace AWSS3Manage.Models
{
    public class RoleDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Role Name")]
        [Required(ErrorMessage = "Role name can't be null !!!")]
        public string? RoleName { get; set; }

        [Display(Name = "Role Description")]
        public string? RoleDescription { get; set; }
    }
}
