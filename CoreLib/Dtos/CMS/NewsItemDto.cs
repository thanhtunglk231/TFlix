using System;

namespace CoreLib.Dtos.CMS
{
    public class NewsItemDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = default!;
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Tin tức chung";
        public int ViewsCount { get; set; }
        public string IsFeaturedYN { get; set; } = "N";
        public bool IsFeatured => string.Equals(IsFeaturedYN, "Y", StringComparison.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
