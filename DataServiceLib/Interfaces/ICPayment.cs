using CoreLib.Dtos.Payment;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICPayment
{
    Task<CResponseMessage> GetPlansAsync();
    Task<CResponseMessage> GetSubscriptionStatusAsync(string email);
    Task<CResponseMessage> CreatePendingAsync(string email, long planId, long orderCode);
    Task<CResponseMessage> CompletePayOsAsync(long orderCode, decimal amount);
    Task<CResponseMessage> FailPayOsAsync(long orderCode);
}

