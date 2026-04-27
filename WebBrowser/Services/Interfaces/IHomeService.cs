using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Home;

namespace WebBrowser.Services.Interfaces
{
    public interface IHomeService
    {
        Task<ApiResponse<MovieLastestTableWrappepr>> get_Movie_Lastest_Item();
        Task<ApiResponse<EpisodeLatestTableWrapper>> get_episode_latest();
        Task<ApiResponse<ContentSearchTableWrapper>> search_contents(string keyword);
    }
}