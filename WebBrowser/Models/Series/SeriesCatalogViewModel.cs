using System;
using System.Collections.Generic;

namespace WebBrowser.Models.Series
{
    public class SeriesCatalogFilter
    {
        public string? Search { get; set; }
        public int? GenreId { get; set; }
        public string? CountryCode { get; set; }
        public int? Year { get; set; }
        public string SortBy { get; set; } = "newest";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class SeriesCatalogViewModel
    {
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public List<SerieDto> Items { get; set; } = new();
        public SeriesCatalogFilter Filter { get; set; } = new();
    }
}
