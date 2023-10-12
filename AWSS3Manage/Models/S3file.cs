using System.ComponentModel.DataAnnotations;

namespace AWSS3Manage.Models
{
    public class S3file
    {
        [Key]
        public int FileId { get; set; }
        public string? FileName { get; set; }
        public string? FileSize { get; set; }
        public string? FileDescription { get; set; }
        public DateTime? UploadDate { get; set; }

        [Required]
        public int? Id { get; set; } // Id -> AppUser's id
        [Required]
        public AppUser? AppUser { get; set; }
    }
}
