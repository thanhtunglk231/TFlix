using CoreLib.Dtos;
using CoreLib.Models;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class AdminAccountService : IAdminAccountService
    {
        private const string BaseUrl = "/api/AdminAccounts";
        private readonly IHttpService _httpService;

        public AdminAccountService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<CResponseMessage> GetManagementDataAsync() =>
            _httpService.GetAsync<CResponseMessage>($"{BaseUrl}/management-data");

        public Task<CResponseMessage> CreateAsync(CreateAdminAccountDto dto) =>
            _httpService.PostAsync<CResponseMessage>(BaseUrl, dto);

        public Task<CResponseMessage> UpdateAsync(UpdateAdminAccountDto dto) =>
            _httpService.PostAsync<CResponseMessage>($"{BaseUrl}/update", dto);

        public Task<CResponseMessage> DeleteAsync(long userId) =>
            _httpService.PostAsync<CResponseMessage>($"{BaseUrl}/delete/{userId}", new { });
    }
}
