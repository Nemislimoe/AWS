using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public ICollection<MediaTag> MediaTags { get; set; } = new List<MediaTag>();
    }
}
