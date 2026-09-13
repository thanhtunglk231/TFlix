using CoreLib.Dtos.CMS;
using CoreLib.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebBrowser.Services.Interfaces
{
    public interface INewsService
    {
        Task<NewsCatalogResultDto> GetCatalogNewsAsync(NewsCatalogFilterDto filter);
        Task<NewsDetailDto?> GetNewsDetailAsync(long id);
        Task<List<NewsCategoryDto>> GetCategoriesAsync();
    }
}
