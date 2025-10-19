using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface IPreView
    {
        CResponseMessage get_all(int movieId);
        CResponseMessage Get_All_Series(int seriesId);
    }
}