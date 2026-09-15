using CoreLib.Dtos.CMS;
using CoreLib.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class NewsService : INewsService
    {
        private readonly IHttpService _httpService;

        public NewsService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<NewsCatalogResultDto> GetCatalogNewsAsync(NewsCatalogFilterDto filter)
        {
            try
            {
                var response = await _httpService.PostAsync<CResponseMessage>("/api/News/catalog", filter);
                if (response != null && response.Data != null)
                {
                    var json = JsonConvert.SerializeObject(response.Data);
                    var result = JsonConvert.DeserializeObject<NewsCatalogResultDto>(json);
                    return result ?? new NewsCatalogResultDto();
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("[NewsService] GetCatalogNewsAsync error: " + ex.Message);
            }
            return new NewsCatalogResultDto();
        }

        public async Task<NewsDetailDto?> GetNewsDetailAsync(long id)
        {
            try
            {
                var response = await _httpService.GetAsync<CResponseMessage>($"/api/News/detail/{id}");
                if (response != null && response.Data != null)
                {
                    var json = JsonConvert.SerializeObject(response.Data);
                    var result = JsonConvert.DeserializeObject<NewsDetailDto>(json);
                    return result;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("[NewsService] GetNewsDetailAsync error: " + ex.Message);
            }
            return null;
        }

        public async Task<List<NewsCategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpService.GetAsync<CResponseMessage>("/api/News/categories");
                if (response != null && response.Data != null)
                {
                    var json = JsonConvert.SerializeObject(response.Data);
                    var list = JsonConvert.DeserializeObject<List<NewsCategoryDto>>(json);
                    return list ?? new List<NewsCategoryDto>();
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("[NewsService] GetCategoriesAsync error: " + ex.Message);
            }
            return new List<NewsCategoryDto>();
        }
    }
}
