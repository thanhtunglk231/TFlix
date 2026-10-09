using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private const string BaseUrl = "/api/admin/subscription-plans";
    private readonly IHttpService _httpService;

    public SubscriptionPlanService(IHttpService httpService) => _httpService = httpService;

    public Task<CResponseMessage> GetAllAsync() => _httpService.GetAsync<CResponseMessage>(BaseUrl);
    public Task<CResponseMessage> CreateAsync(CreateSubscriptionPlanDto dto) =>
        _httpService.PostAsync<CResponseMessage>(BaseUrl, dto);
    public Task<CResponseMessage> UpdateAsync(UpdateSubscriptionPlanDto dto) =>
        _httpService.PutResponseAsync($"{BaseUrl}/{dto.PlanId}", dto);
    public Task<CResponseMessage> DeleteAsync(long planId) =>
        _httpService.DeleteResponseAsync($"{BaseUrl}/{planId}");
}
