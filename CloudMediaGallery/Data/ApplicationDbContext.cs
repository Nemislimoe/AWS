using Azure;
using CloudMediaGallery.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Data
{
    // Контекст бази даних: Identity + наші сутності
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<MediaFile> MediaFiles { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<MediaTag> MediaTags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Composite key для зв'язку MediaTag
            builder.Entity<MediaTag>()
                .HasKey(mt => new { mt.MediaFileId, mt.TagId });

            builder.Entity<MediaTag>()
                .HasOne(mt => mt.MediaFile)
                .WithMany(m => m.MediaTags)
                .HasForeignKey(mt => mt.MediaFileId);

            builder.Entity<MediaTag>()
                .HasOne(mt => mt.Tag)
                .WithMany(t => t.MediaTags)
                .HasForeignKey(mt => mt.TagId);
        }
    }
}
