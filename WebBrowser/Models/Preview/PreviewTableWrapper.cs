using Newtonsoft.Json;

namespace WebBrowser.Models.Preview
{
    public class PreviewTableWrapper
    {
        [JsonProperty("table")]
        public List<PreviewItem> Table { get; set; } = new();
    }
}
