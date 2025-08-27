using CoreLib.Dtos.Season;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICSeason
    {
        Task<CResponseMessage> Add_season(AddSeasonDto dto);
        Task<CResponseMessage> Delete_season(decimal seasonId);
        Task<CResponseMessage> get_all();
        Task<CResponseMessage> Update_season(UpdateSeasonDto dto);
    }
}