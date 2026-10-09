using CoreLib.Dtos;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICAdminAccount
    {
        Task<CResponseMessage> GetManagementDataAsync();
        Task<CResponseMessage> CreateAsync(CreateAdminAccountDto dto);
        Task<CResponseMessage> UpdateAsync(UpdateAdminAccountDto dto);
        Task<CResponseMessage> DeleteAsync(long userId);
    }
}
