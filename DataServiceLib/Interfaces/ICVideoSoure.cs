using CoreLib.Dtos.VideSoure;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICVideoSoure
    {
        Task<CResponseMessage> Add_video_source(AddVideoSourceDto dto);
        Task<CResponseMessage> Delete_video_source(decimal sourceId);
        Task<CResponseMessage> Update_video_source(UpdateVideoSourceDto dto);
        CResponseMessage get_all();
    }
}