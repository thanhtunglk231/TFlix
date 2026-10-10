using CoreLib.Dtos.Subscription;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICAccountSubscription
{
    Task<CResponseMessage> GetManagementDataAsync();
    Task<CResponseMessage> CreateAsync(CreateAccountSubscriptionDto dto);
    Task<CResponseMessage> UpdateAsync(UpdateAccountSubscriptionDto dto);
    Task<CResponseMessage> DeleteAsync(long subscriptionId);
}
