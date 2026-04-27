using WebBrowser.Models;
using WebBrowser.Models.Home;

namespace WebBrowser.Services.Interfaces
{
    public interface IHomeService
    {
        Task<ApiResponse<MovieLastestTableWrappepr>> get_Movie_Lastest_Item();
    }
}