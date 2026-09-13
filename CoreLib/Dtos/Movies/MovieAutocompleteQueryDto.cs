namespace CoreLib.Dtos.Movies
{
    public class MovieAutocompleteQueryDto
    {
        public string Query { get; set; } = string.Empty;
        public int Limit { get; set; } = 8;
    }
}
