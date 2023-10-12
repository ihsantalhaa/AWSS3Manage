namespace AWSS3Manage.Models
{
    public class RoleUserViewModel
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? RoleDescription { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserDescription { get; set; }
    }
}
