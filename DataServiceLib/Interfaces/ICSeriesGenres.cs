using CoreLib.Dtos.SeriesGenre;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICSeriesGenres
    {
        Task<CResponseMessage> sp_add(AddSeriesGenreDto addSeriesGenreDto);
        Task<CResponseMessage> sp_delete(DeleteSeriesGenreDto deleteSeriesGenreDto);
        Task<CResponseMessage> sp_get_all();
        Task<CResponseMessage> sp_get_by_genre(decimal genreId);
        Task<CResponseMessage> sp_get_by_pk(decimal seriesId, decimal genreId);
        Task<CResponseMessage> sp_get_by_series(decimal seriesId);
        Task<CResponseMessage> sp_update(UpdateSeriesGenreDto updateSeriesGenreDto);
    }
}