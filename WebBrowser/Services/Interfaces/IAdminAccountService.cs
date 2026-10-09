using CoreLib.Dtos;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces
{
    public interface IAdminAccountService
    {
        Task<CResponseMessage> GetManagementDataAsync();
        Task<CResponseMessage> CreateAsync(CreateAdminAccountDto dto);
        Task<CResponseMessage> UpdateAsync(UpdateAdminAccountDto dto);
        Task<CResponseMessage> DeleteAsync(long userId);
    }
}
