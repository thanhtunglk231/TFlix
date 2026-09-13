using CoreLib.Dtos;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<CResponseMessage> GetCurrentPermissions(string? screenCode);
        Task<CResponseMessage> GetMatrix();
        Task<CResponseMessage> SetRolePermission(RolePermissionSetDto dto);
    }
}
