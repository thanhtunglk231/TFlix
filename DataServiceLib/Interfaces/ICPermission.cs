using CoreLib.Dtos;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICPermission
    {
        Task<CResponseMessage> GetAll();
        Task<CResponseMessage> GetMatrix();
        Task<CResponseMessage> SetRolePermission(RolePermissionSetDto dto);
        Task<CResponseMessage> GetUserPermissions(string email, string? screenCode);
        Task<CResponseMessage> CheckUserPermission(CheckPermissionDto dto);
    }
}
