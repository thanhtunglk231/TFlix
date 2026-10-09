using CoreLib.Dtos.Payment;
using CoreLib.Models;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public sealed class PaymentService : IPaymentService
{
    private const string BaseUrl = "/api/payments";
    private readonly IHttpService _httpService;

    public PaymentService(IHttpService httpService) => _httpService = httpService;
    public Task<CResponseMessage> GetPlansAsync() => _httpService.GetAsync<CResponseMessage>($"{BaseUrl}/plans");
    public Task<CResponseMessage> GetSubscriptionStatusAsync() => _httpService.GetAsync<CResponseMessage>($"{BaseUrl}/subscription-status");
    public async Task<PayOsCheckoutResultDto?> CheckoutAsync(CheckoutPaymentDto dto)
    {
        var response = await _httpService.PostAsync<PayOsResponse>($"{BaseUrl}/checkout", dto);
        return response?.Success == true ? response.Data : null;
    }
    public Task<CResponseMessage> ConfirmPayOsAsync(long orderCode) =>
        _httpService.PostAsync<CResponseMessage>($"{BaseUrl}/payos/confirm/{orderCode}", new { });

    private sealed class PayOsResponse
    {
        public bool Success { get; set; }
        public PayOsCheckoutResultDto? Data { get; set; }
    }
}

