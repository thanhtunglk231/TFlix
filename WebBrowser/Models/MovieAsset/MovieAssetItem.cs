namespace WebBrowser.Models.MovieAsset
{
    public class MovieAssetItem
    {
        public decimal AssetId { get; set; }
        public decimal MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public string AssetType { get; set; } = default!;  // STILL|THUMB|TRAILER
        public string Url { get; set; } = default!;
        public int? SortOrder { get; set; }
    }
}
