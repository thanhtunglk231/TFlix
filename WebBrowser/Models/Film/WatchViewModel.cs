using WebBrowser.Models.Episode;
using WebBrowser.Models.Preview;
using WebBrowser.Models.VideoSoure;

namespace WebBrowser.Models.Film
{
    public class WatchViewModel
    {
        public long ContentId { get; set; }
        public PreviewItem Content { get; set; } = new();
        public List<EpisodeItem> Episodes { get; set; } = new();
        public List<SourceItem> Sources { get; set; } = new();
        public long? CurrentEpisodeId { get; set; }
        public string? EpisodeLoadError { get; set; }
        public bool IsPremiumContent { get; set; }
        public bool HasActiveSubscription { get; set; }
        public DateTimeOffset? SubscriptionEndAt { get; set; }
    }
}
