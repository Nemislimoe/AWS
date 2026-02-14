using CloudMediaGallery.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Основні таблиці
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<MediaTag> MediaTags { get; set; }

        // Нові таблиці для розширеної функціональності
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionMedia> CollectionMedias { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<CollectionSubscription> CollectionSubscriptions { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<UserFavoriteTag> UserFavoriteTags { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Налаштування MediaTag (many-to-many між MediaFile та Tag)
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

            // Налаштування CollectionMedia (many-to-many між Collection та MediaFile)
            builder.Entity<CollectionMedia>()
                .HasKey(cm => new { cm.CollectionId, cm.MediaFileId });

            builder.Entity<CollectionMedia>()
                .HasOne(cm => cm.Collection)
                .WithMany(c => c.CollectionMedias)
                .HasForeignKey(cm => cm.CollectionId);

            builder.Entity<CollectionMedia>()
                .HasOne(cm => cm.MediaFile)
                .WithMany(m => m.CollectionMedias)
                .HasForeignKey(cm => cm.MediaFileId);

            // Налаштування UserFavoriteTag (many-to-many між UserPreference та Tag)
            builder.Entity<UserFavoriteTag>()
                .HasKey(uft => new { uft.UserPreferenceId, uft.TagId });

            builder.Entity<UserFavoriteTag>()
                .HasOne(uft => uft.UserPreference)
                .WithMany(up => up.FavoriteTags)
                .HasForeignKey(uft => uft.UserPreferenceId);

            builder.Entity<UserFavoriteTag>()
                .HasOne(uft => uft.Tag)
                .WithMany()
                .HasForeignKey(uft => uft.TagId);

            // Налаштування Like - унікальна комбінація користувача та медіафайлу
            builder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.MediaFileId })
                .IsUnique();

            // Налаштування Rating - унікальна комбінація користувача та медіафайлу
            builder.Entity<Rating>()
                .HasIndex(r => new { r.UserId, r.MediaFileId })
                .IsUnique();

            // Налаштування CollectionSubscription - унікальна підписка
            builder.Entity<CollectionSubscription>()
                .HasIndex(cs => new { cs.SubscriberId, cs.CollectionId })
                .IsUnique();

            // Налаштування Comment - self-referencing для threaded comments
            builder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування каскадного видалення
            builder.Entity<MediaFile>()
                .HasMany(m => m.Comments)
                .WithOne(c => c.MediaFile)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MediaFile>()
                .HasMany(m => m.Likes)
                .WithOne(l => l.MediaFile)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MediaFile>()
                .HasMany(m => m.Ratings)
                .WithOne(r => r.MediaFile)
                .OnDelete(DeleteBehavior.Cascade);

            // Індекси для покращення продуктивності пошуку
            builder.Entity<MediaFile>()
                .HasIndex(m => m.UploadedAt);

            builder.Entity<MediaFile>()
                .HasIndex(m => m.MediaType);

            builder.Entity<MediaFile>()
                .HasIndex(m => m.IsBlocked);

            builder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.SearchedAt);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.IsBlocked);
        }
    }
}
