namespace WebBrowser.Models.Home
{
    public class ContentSearchTableWrapper
    {
        public List<ContentSearchItem> Table { get; set; } = new();
    }

    public class ContentSearchItem
    {
        public string content_type { get; set; }   // MOVIE / EPISODE
        public int content_id { get; set; }

        public int? movie_id { get; set; }
        public int? series_id { get; set; }
        public int? season_id { get; set; }
        public int? episode_id { get; set; }

        public string title { get; set; }
        public string original_title { get; set; }
        public string overview { get; set; }

        public DateTime? publish_date { get; set; }

        public int? season_no { get; set; }
        public int? episode_no { get; set; }

        public int? duration_min { get; set; }
        public string age_rating { get; set; }

        public string poster_url { get; set; }
        public string backdrop_url { get; set; }
        public string trailer_url { get; set; }

        public string genres_json { get; set; }
        public string assets_json { get; set; }
        public string cast_json { get; set; }
        public string crew_json { get; set; }
        public string sources_json { get; set; }

        public double? avg_rating { get; set; }
        public int? rating_count { get; set; }
        public int? comment_count { get; set; }
    }
}
