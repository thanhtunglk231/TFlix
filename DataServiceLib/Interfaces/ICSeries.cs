using CoreLib.Dtos.Series;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICSeries
    {
        Task<CResponseMessage> Add_series(AddSeriesDto dto);
        Task<CResponseMessage> Delete_series(decimal seriesId);
        Task<CResponseMessage> get_all();
        Task<CResponseMessage> Update_series(UpdateSeriesDto dto);
    }
}