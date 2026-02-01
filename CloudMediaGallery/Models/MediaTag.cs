namespace CloudMediaGallery.Models
{
    // Зв'язуюча сутність між MediaFile та Tag
    public class MediaTag
    {
        public int MediaFileId { get; set; }
        public MediaFile? MediaFile { get; set; }

        public int TagId { get; set; }
        public Tag? Tag { get; set; }
    }
}
