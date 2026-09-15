using System;
using System.Collections.Generic;

namespace CoreLib.Dtos.CMS
{
    public class NewsDetailDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = default!;
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? ContentHtml { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Tin tức chung";
        public int ViewsCount { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<NewsItemDto> RelatedPosts { get; set; } = new List<NewsItemDto>();
    }
}
