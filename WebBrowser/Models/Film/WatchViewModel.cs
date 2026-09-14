using WebBrowser.Models.Episode;
using WebBrowser.Models.Preview;
using WebBrowser.Models.VideoSoure;

namespace WebBrowser.Models.Film
{
    public class WatchViewModel
    {
        public PreviewItem Content { get; set; } = new();
        public List<EpisodeItem> Episodes { get; set; } = new();
        public List<SourceItem> Sources { get; set; } = new();
        public int? CurrentEpisodeId { get; set; }
    }
}
