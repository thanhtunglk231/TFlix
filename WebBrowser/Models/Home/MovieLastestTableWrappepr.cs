using Newtonsoft.Json;

namespace WebBrowser.Models.Home
{
    public class MovieLastestTableWrappepr
    {
        [JsonProperty("table")]
        public List<MovieLastestItem>? Table { get; set; } = new();
    }
}
