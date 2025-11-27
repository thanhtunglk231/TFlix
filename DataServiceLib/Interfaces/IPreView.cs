using CoreLib.Dtos.Preview;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface IPreView
    {
        CResponseMessage get_all(int movieId);
        CResponseMessage Get_All_Series(int seriesId);
        CResponseMessage GET_CONTENT_BY_ID(GETCONTENTByID movie);
    }
}