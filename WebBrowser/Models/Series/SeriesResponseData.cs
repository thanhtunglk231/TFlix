using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace WebBrowser.Models.Series
{
    public class SeriesResponseData
    {
        [JsonProperty("table")]
        public List<SerieDto> Table { get; set; } = new();
    }
}
