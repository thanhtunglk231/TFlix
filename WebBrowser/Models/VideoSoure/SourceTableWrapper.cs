using Newtonsoft.Json;

namespace WebBrowser.Models.VideoSoure
{
    public class SourceTableWrapper
    {
        [JsonProperty("table")]
        public List<SourceItem> Table { get; set; } = new();
    }
}
