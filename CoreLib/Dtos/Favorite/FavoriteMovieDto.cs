namespace CoreLib.Dtos.Favorite;

public class FavoriteMovieDto
{
    public long MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? DurationMin { get; set; }
    public string? AgeRating { get; set; }
    public string? PosterUrl { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

public class FavoriteMovieRequest
{
    public long UserId { get; set; }
    public long MovieId { get; set; }
}

public class FavoriteToggleResult
{
    public long MovieId { get; set; }
    public bool IsFavorite { get; set; }
}
