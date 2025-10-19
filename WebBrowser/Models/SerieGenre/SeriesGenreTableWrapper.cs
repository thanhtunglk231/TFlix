using Newtonsoft.Json;
using WebBrowser.Models.MovieAsset;

namespace WebBrowser.Models.SerieGenre
{
    public class SeriesGenreTableWrapper
    {
        [JsonProperty("table")]
        public List<SeriesGenreItem> Table { get; set; } = new();
    }
}
