using CoreLib.Dtos.Subscription;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICSubscriptionPlan
{
    Task<CResponseMessage> GetAllAsync();
    Task<CResponseMessage> CreateAsync(CreateSubscriptionPlanDto dto);
    Task<CResponseMessage> UpdateAsync(UpdateSubscriptionPlanDto dto);
    Task<CResponseMessage> DeleteAsync(long planId);
}
