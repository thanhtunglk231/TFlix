using CoreLib.Dtos.Payment;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces;

public interface IPaymentService
{
    Task<CResponseMessage> GetPlansAsync();
    Task<CResponseMessage> GetSubscriptionStatusAsync();
    Task<PayOsCheckoutResultDto?> CheckoutAsync(CheckoutPaymentDto dto);
    Task<CResponseMessage> ConfirmPayOsAsync(long orderCode);
}

