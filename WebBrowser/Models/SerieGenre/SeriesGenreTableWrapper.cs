using Newtonsoft.Json;

namespace WebBrowser.Models.SerieGenre
{
    public class SeriesGenreTableWrapper
    {
        [JsonProperty("table")]
        public List<SeriesGenreItem> Table { get; set; } = new();
    }
}