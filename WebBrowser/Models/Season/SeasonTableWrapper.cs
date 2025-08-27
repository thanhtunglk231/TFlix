using Newtonsoft.Json;

namespace WebBrowser.Models.Season
{
    public class SeasonTableWrapper
    {
        [JsonProperty("table")]
        public List<SeasonItem> Table { get; set; } = new();
    }
}
