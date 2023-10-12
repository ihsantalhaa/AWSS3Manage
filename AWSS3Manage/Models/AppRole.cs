using Microsoft.AspNetCore.Identity;

namespace AWSS3Manage.Models
{
    public class AppRole : IdentityRole<int>
    {
        public string? RoleDescription { get; set; }
    }
}
