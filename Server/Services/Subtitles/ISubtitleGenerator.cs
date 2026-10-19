using CoreLib.Models;

namespace Server.Services.Subtitles;

public interface ISubtitleGenerator
{
    Task<CResponseMessage> GenerateAsync(long sourceId, CancellationToken cancellationToken = default);
}
