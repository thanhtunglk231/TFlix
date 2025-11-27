using CoreLib.Dtos;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICFilm
    {
        CResponseMessage Get_Film_Detail(GetFilmDetail filmId);
    }
}