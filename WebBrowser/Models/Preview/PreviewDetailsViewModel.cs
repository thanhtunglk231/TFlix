using System.Collections.Generic;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;

namespace WebBrowser.Models.Preview
{
    public class PreviewDetailsViewModel
    {
        public PreviewItem Movie { get; set; } = new();
        public List<MovieItem> UpcomingMovies { get; set; } = new();
        public List<MovieItem> TrendingMovies { get; set; } = new();
        public List<GenreItem> HotTags { get; set; } = new();
        public List<EpisodeItem> Episodes { get; set; } = new();
        public CoreLib.Dtos.Rating.MovieRatingDto? RatingInfo { get; set; }
    }
}

