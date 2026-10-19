using CoreLib.Dtos.Subtitle;
using Newtonsoft.Json;

namespace WebBrowser.Models.Subtitle;

public sealed class SubtitleTableWrapper
{
    [JsonProperty("table")]
    public List<SubtitleDto> Table { get; set; } = new();
}
