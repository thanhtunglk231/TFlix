using CoreLib.Dtos.VideoHighlight;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICVideoHighlight
{
    Task<CResponseMessage> GetAllAsync(int page = 1, int pageSize = 10, bool activeOnly = false);
    Task<CResponseMessage> GetBySourceAsync(long sourceId);
    Task<CResponseMessage> AddAsync(CreateVideoHighlightDto dto);
    Task<CResponseMessage> DeleteAsync(long highlightId);
}
