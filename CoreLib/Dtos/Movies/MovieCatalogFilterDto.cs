using System;

namespace CoreLib.Dtos.Movies
{
    public class MovieCatalogFilterDto
    {
        public string? Search { get; set; }
        public int? GenreId { get; set; }
        public string? CountryCode { get; set; }
        public int? Year { get; set; }
        public string SortBy { get; set; } = "newest";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class MovieCatalogResultDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public List<MovieCatalogItemDto> Items { get; set; } = new List<MovieCatalogItemDto>();
    }
}
