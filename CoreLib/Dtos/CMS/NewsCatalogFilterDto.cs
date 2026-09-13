namespace CoreLib.Dtos.CMS
{
    public class NewsCatalogFilterDto
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}
