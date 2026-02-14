using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Підписка користувача на колекцію
    /// </summary>
    public class CollectionSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CollectionId { get; set; }
        public Collection? Collection { get; set; }

        [Required]
        public string SubscriberId { get; set; } = "";
        public ApplicationUser? Subscriber { get; set; }

        [Required]
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}
