using CoreLib.Models;

namespace Server.Services.VideoHighlights;

public interface IVideoHighlightGenerator
{
    Task<CResponseMessage> GenerateAsync(long sourceId, CancellationToken cancellationToken = default);
}
