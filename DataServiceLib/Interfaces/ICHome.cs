using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICHome
    {
        Task<CResponseMessage> MovieLastestItem();
        Task<CResponseMessage> EpisodeLatestItem(int limit = 10);
        CResponseMessage Search(string keyword);
    }
}