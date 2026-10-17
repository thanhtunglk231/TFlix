using CoreLib.Dtos.Series;
using CoreLib.Models;
using WebBrowser.Areas.Admin.Models;
using WebBrowser.Models;
using WebBrowser.Models.Series;

namespace WebBrowser.Services.Interfaces
{
    public interface ISeriesService
    {
        Task<CResponseMessage> add_series(AddSeriesDto addSeriesDto);
        Task<CResponseMessage> delete_series(decimal id);
        Task<ApiResponse<SeriesResponseData>> get_all();
        Task<CResponseMessage> uppdate_series(UpdateSeriesDto addSeriesDto);
    }
}