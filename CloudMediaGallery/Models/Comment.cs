using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Коментар до зображення
    /// </summary>
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int MediaFileId { get; set; }
        public MediaFile? MediaFile { get; set; }

        [Required]
        public string AuthorId { get; set; } = "";
        public ApplicationUser? Author { get; set; }

        /// <summary>
        /// Батьківський коментар для відповідей (threaded comments)
        /// </summary>
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }

        /// <summary>
        /// Відповіді на цей коментар
        /// </summary>
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
