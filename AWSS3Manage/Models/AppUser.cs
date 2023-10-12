using Microsoft.AspNetCore.Identity;

namespace AWSS3Manage.Models
{
    public class AppUser : IdentityUser<int>
    {
        public string? UserDescription { get; set; }
        public ICollection<S3file>? S3files { get; set; }
    }
}
