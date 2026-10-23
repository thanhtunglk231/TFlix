// CoreLib/Dtos/Episode/AssignMovieToSeasonDto.cs
namespace CoreLib.Dtos.Episode
{
    public class AssignMovieToSeasonDto
    {
        public long SeriesId   { get; set; }
        public long SeasonId   { get; set; }
        public long MovieId    { get; set; }
        public int  EpisodeNo  { get; set; }
    }

    public class RemoveMovieFromSeasonDto
    {
        public long EpisodeId  { get; set; }
    }

    public class UpdateMovieInSeasonDto
    {
        public long EpisodeId  { get; set; }
        public int  EpisodeNo  { get; set; }
    }

    public class SeasonMovieItemDto
    {
        public long   EpisodeId           { get; set; }
        public int    EpisodeNo           { get; set; }
        public long   SeriesId            { get; set; }
        public long   SeasonId            { get; set; }
        public long?  MovieId             { get; set; }
        public string? MovieTitle         { get; set; }
        public string? MovieOriginalTitle { get; set; }
        public int?   DurationMin         { get; set; }
        public string? MovieStatus        { get; set; }
        public string? IsPremium          { get; set; }
        public string? EpisodeStatus      { get; set; }
        public DateTime? CreatedAt        { get; set; }
        public string? PosterUrl          { get; set; }
    }
}
