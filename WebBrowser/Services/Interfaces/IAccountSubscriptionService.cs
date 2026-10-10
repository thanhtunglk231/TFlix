using CoreLib.Dtos.Subscription;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces;

public interface IAccountSubscriptionService
{
    Task<CResponseMessage> GetManagementDataAsync();
    Task<CResponseMessage> CreateAsync(CreateAccountSubscriptionDto dto);
    Task<CResponseMessage> UpdateAsync(UpdateAccountSubscriptionDto dto);
    Task<CResponseMessage> DeleteAsync(long subscriptionId);
}
