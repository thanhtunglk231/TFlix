namespace CoreLib.Dtos.CMS
{
    public class NewsCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
        public string? Slug { get; set; }
    }
}
