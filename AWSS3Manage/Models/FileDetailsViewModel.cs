using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace AWSS3Manage.Models
{
    public class FileDetailsViewModel
    {
        public int? FileId { get; set; }
        public string? FileUserName { get; set; }

        [Display(Name = "File Name")]
        [Required(ErrorMessage = "File name can't be null !!!")]
        public string? FileName { get; set; }
        public string? FileSize { get; set; }
        public string? FileDescription { get; set; }
        public DateTime? UploadDate { get; set; }
    }
}
