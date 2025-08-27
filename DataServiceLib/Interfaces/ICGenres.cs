using CoreLib.Dtos.Genres;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICGenres
    {

        Task<CResponseMessage> GetAll();
        Task<CResponseMessage> Add(AddGenreDto dto);
        Task<CResponseMessage> Update(UpdateGenreDto dto);
        Task<CResponseMessage> Delete(decimal genreId);
    }
}