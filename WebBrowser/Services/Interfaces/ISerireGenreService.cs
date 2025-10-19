using CoreLib.Dtos.SeriesGenre;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.SerieGenre;

namespace WebBrowser.Services.Interfaces
{
    public interface ISerireGenreService
    {
        Task<CResponseMessage> add_MovieGenre(AddSeriesGenreDto addSeriesDto);
        Task<CResponseMessage> delete_Episode(DeleteSeriesGenreDto id);
        Task<ApiResponse<SeriesGenreTableWrapper>> get_byid(decimal id);
        Task<CResponseMessage> uppdate_MovieGenre(UpdateSeriesGenreDto updateDto);
    }
}