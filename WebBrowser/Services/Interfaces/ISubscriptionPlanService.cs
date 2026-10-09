using CoreLib.Dtos.Subscription;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces;

public interface ISubscriptionPlanService
{
    Task<CResponseMessage> GetAllAsync();
    Task<CResponseMessage> CreateAsync(CreateSubscriptionPlanDto dto);
    Task<CResponseMessage> UpdateAsync(UpdateSubscriptionPlanDto dto);
    Task<CResponseMessage> DeleteAsync(long planId);
}
