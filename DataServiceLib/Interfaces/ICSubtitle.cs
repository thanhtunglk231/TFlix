using CoreLib.Dtos.Subtitle;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICSubtitle
{
    Task<CResponseMessage> GetBySourceAsync(long sourceId);
    Task<CResponseMessage> UpsertAsync(CreateSubtitleDto dto);
}
