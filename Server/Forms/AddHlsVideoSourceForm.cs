namespace Server.Forms
{
    public class AddHlsVideoSourceForm
    {
        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }

        public IFormFile Playlist { get; set; } = default!;         // playlist.m3u8
        public List<IFormFile> Segments { get; set; } = new();
    }
}
