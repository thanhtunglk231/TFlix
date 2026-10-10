namespace CoreLib.Dtos.Rating;

public class MovieRatingDto
{
    public long MovieId { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int? UserRating { get; set; }
}

public class SetMovieRatingRequest
{
    public long UserId { get; set; }
    public long MovieId { get; set; }
    public int RatingVal { get; set; }
}

public class MovieRatingResult
{
    public long MovieId { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int UserRating { get; set; }
}
