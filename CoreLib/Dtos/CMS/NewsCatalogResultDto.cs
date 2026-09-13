using System.Collections.Generic;

namespace CoreLib.Dtos.CMS
{
    public class NewsCatalogResultDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalCount / PageSize) : 0;
        public List<NewsItemDto> Items { get; set; } = new List<NewsItemDto>();
    }
}
