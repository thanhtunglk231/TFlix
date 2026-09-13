namespace CoreLib.Dtos.Movies
{
    public class MovieAutocompleteItemDto
    {
        public int MovieId { get; set; }
        public string? Title { get; set; }
        public string? OriginalTitle { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? ProducerName { get; set; }
        public string? PosterUrl { get; set; }
        public string Kind { get; set; } = "MOVIE";
    }
}
