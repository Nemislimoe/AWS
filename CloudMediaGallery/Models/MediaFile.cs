using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    public class MediaFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = "";

        [Required]
        public string ContentType { get; set; } = "";

        [Required]
        public string MediaType { get; set; } = ""; // "image" або "video"

        [Required]
        public DateTime UploadedAt { get; set; }

        [Required]
        public string BlobUrl { get; set; } = "";

        [Required]
        public string UploadedById { get; set; } = "";
        public ApplicationUser? UploadedBy { get; set; }

        public ICollection<MediaTag> MediaTags { get; set; } = new List<MediaTag>();
    }
}
