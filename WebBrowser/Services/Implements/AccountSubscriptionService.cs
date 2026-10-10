using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public sealed class AccountSubscriptionService : IAccountSubscriptionService
{
    private const string BaseUrl = "/api/admin/account-subscriptions";
    private readonly IHttpService _httpService;

    public AccountSubscriptionService(IHttpService httpService) => _httpService = httpService;

    public Task<CResponseMessage> GetManagementDataAsync() => _httpService.GetAsync<CResponseMessage>(BaseUrl);
    public Task<CResponseMessage> CreateAsync(CreateAccountSubscriptionDto dto) => _httpService.PostAsync<CResponseMessage>(BaseUrl, dto);
    public Task<CResponseMessage> UpdateAsync(UpdateAccountSubscriptionDto dto) => _httpService.PutResponseAsync($"{BaseUrl}/{dto.SubscriptionId}", dto);
    public Task<CResponseMessage> DeleteAsync(long subscriptionId) => _httpService.DeleteResponseAsync($"{BaseUrl}/{subscriptionId}");
}
