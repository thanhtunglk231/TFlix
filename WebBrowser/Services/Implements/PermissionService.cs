using CoreLib.Dtos;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class PermissionService : IPermissionService
    {
        private const string BASE = "/api/Permissions";
        private readonly IHttpService _httpService;

        public PermissionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<CResponseMessage> GetCurrentPermissions(string? screenCode)
        {
            var url = string.IsNullOrWhiteSpace(screenCode)
                ? $"{BASE}/my"
                : $"{BASE}/my?screenCode={Uri.EscapeDataString(screenCode)}";

            try
            {
                return await _httpService.GetAsync<CResponseMessage>(url);
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "403",
                    message = "Không lấy được quyền hiện tại: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> GetMatrix()
        {
            try
            {
                return await _httpService.GetAsync<CResponseMessage>($"{BASE}/matrix");
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Không lấy được ma trận phân quyền: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> SetRolePermission(RolePermissionSetDto dto)
        {
            Console.WriteLine("[PermissionService] SetRolePermission payload: " + JsonConvert.SerializeObject(dto));
            return await _httpService.PostAsync<CResponseMessage>($"{BASE}/set-role-permission", dto);
        }
    }
}
