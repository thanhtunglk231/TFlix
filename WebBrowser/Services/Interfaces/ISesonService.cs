using CoreLib.Dtos.Season;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Season;

namespace WebBrowser.Services.Interfaces
{
    public interface ISesonService
    {
        Task<CResponseMessage> add_season(AddSeasonDto addSeriesDto);
        Task<CResponseMessage> delete_Season(decimal id);
        Task<ApiResponse<SeasonTableWrapper>> get_all();
        Task<CResponseMessage> uppdate_Season(UpdateSeasonDto updateDto);
    }
}