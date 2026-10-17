using CoreLib.Dtos;
using CoreLib.Dtos.VideSoure;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICVideoSoure
    {
        Task<CResponseMessage> Add_video_source(AddVideoSourceDto dto);
        Task<CResponseMessage> Delete_video_source(decimal sourceId);
        Task<CResponseMessage> Get_storage_urls(decimal sourceId);
        Task<CResponseMessage> Update_video_source(UpdateVideoSourceDto dto);
        CResponseMessage get_all(int page = 1, int pageSize = 10, string? search = null,
            string? sourceType = null, long? movieId = null, long? episodeId = null, string? quality = null);
        CResponseMessage get_bu_id(int id);
        CResponseMessage GetBySourceId(long sourceId);
        Task<CResponseMessage> Add_video_source_part(AddVideoSourcePartDto dto);
        Task<CResponseMessage> Replace_video_source_parts(decimal sourceId, IReadOnlyList<AddVideoSourcePartDto> parts);
    }
}
