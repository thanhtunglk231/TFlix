using Newtonsoft.Json;

namespace WebBrowser.Models
{
    public class ApiEnvelope<T>
    {
        [JsonProperty("result")]
        public ApiResponse<T>? Result { get; set; }

        // (optional) meta
        [JsonProperty("id")] public int? Id { get; set; }
        [JsonProperty("status")] public int? Status { get; set; }
    }
}
