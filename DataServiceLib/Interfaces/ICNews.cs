using CoreLib.Dtos.CMS;
using CoreLib.Models;
using System.Threading.Tasks;

namespace DataServiceLib.Interfaces
{
    public interface ICNews
    {
        Task<CResponseMessage> GetCatalogNews(NewsCatalogFilterDto filter);
        Task<CResponseMessage> GetNewsDetail(long postId);
        Task<CResponseMessage> GetCategories();
        Task<CResponseMessage> SeedSampleNews();
    }
}
