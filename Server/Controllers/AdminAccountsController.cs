using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly ICAdminAccount _adminAccount;
        private readonly ICPermission _permission;
        private readonly ILogger<AdminAccountsController> _logger;

        public AdminAccountsController(ICAdminAccount adminAccount, ICPermission permission, ILogger<AdminAccountsController> logger)
        {
            _adminAccount = adminAccount;
            _permission = permission;
            _logger = logger;
        }

        [HttpGet("management-data")]
        public async Task<IActionResult> GetManagementData()
        {
            _logger.LogInformation("Admin account API management data request started by {Actor}", GetActor());
            var forbidden = await RequirePermissionAsync("View");
            if (forbidden != null)
            {
                _logger.LogWarning("Admin account API management data request forbidden for {Actor}", GetActor());
                return forbidden;
            }

            var result = await _adminAccount.GetManagementDataAsync();
            _logger.LogInformation("Admin account API management data request completed with code {Code} and success {Success}", result.code, result.Success);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdminAccountDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Success = false, code = "400", message = "Thông tin tài khoản không hợp lệ." });
            }

            _logger.LogInformation("Admin account API create request started by {Actor} for {EmailDomain} with {RoleCount} roles", GetActor(), GetEmailDomain(dto.Email), dto.RoleIds.Count);
            var forbidden = await RequirePermissionAsync("Create");
            if (forbidden != null)
            {
                _logger.LogWarning("Admin account API create request forbidden for {Actor}", GetActor());
                return forbidden;
            }

            var result = await _adminAccount.CreateAsync(dto);
            _logger.LogInformation("Admin account API create request completed with code {Code} and success {Success}", result.code, result.Success);
            return result.code == "409" ? Conflict(result) : Ok(result);
        }

        [HttpPut]
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateAdminAccountDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Success = false, code = "400", message = "Thông tin tài khoản không hợp lệ." });
            }

            _logger.LogInformation("Admin account API update request started by {Actor} for user {UserId}", GetActor(), dto.UserId);
            var forbidden = await RequirePermissionAsync("Update");
            if (forbidden != null)
            {
                _logger.LogWarning("Admin account API update request forbidden for {Actor}", GetActor());
                return forbidden;
            }

            var result = await _adminAccount.UpdateAsync(dto);
            _logger.LogInformation("Admin account API update request completed with code {Code} and success {Success}", result.code, result.Success);
            return result.code == "409" ? Conflict(result) : Ok(result);
        }

        [HttpDelete("{userId}")]
        [HttpPost("delete/{userId}")]
        public async Task<IActionResult> Delete(long userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { Success = false, code = "400", message = "ID tài khoản không hợp lệ." });
            }

            _logger.LogInformation("Admin account API delete request started by {Actor} for user {UserId}", GetActor(), userId);
            var forbidden = await RequirePermissionAsync("Delete");
            if (forbidden != null)
            {
                _logger.LogWarning("Admin account API delete request forbidden for {Actor}", GetActor());
                return forbidden;
            }

            var result = await _adminAccount.DeleteAsync(userId);
            _logger.LogInformation("Admin account API delete request completed with code {Code} and success {Success}", result.code, result.Success);
            return result.code == "403" ? StatusCode(StatusCodes.Status403Forbidden, result) : Ok(result);
        }

        private string GetActor() => User.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";

        private static string GetEmailDomain(string email) =>
            email.Contains('@') ? email[(email.IndexOf('@') + 1)..] : "invalid";

        private async Task<IActionResult?> RequirePermissionAsync(string permissionCode)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { Success = false, code = "403", message = "Token không có email hợp lệ." });
            }

            var result = await _permission.CheckUserPermission(new CheckPermissionDto
            {
                Email = email,
                ScreenCode = "AdminAccounts",
                PermissionCode = permissionCode
            });

            return result.Success
                ? null
                : StatusCode(StatusCodes.Status403Forbidden,
                    new { Success = false, code = "403", message = $"Không đủ quyền {permissionCode} tài khoản quản trị." });
        }
    }
}
