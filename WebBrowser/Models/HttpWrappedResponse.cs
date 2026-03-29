using Newtonsoft.Json;

namespace WebBrowser.Models
{
    public class HttpWrappedResponse<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}
