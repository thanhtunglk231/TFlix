using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly ICPermission _permission;
        private readonly IRedisCacheService _cache;

        public PermissionsController(ICPermission permission, IRedisCacheService cache)
        {
            _permission = permission;
            _cache = cache;
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var forbidden = await RequirePermission("View");
            if (forbidden != null) return forbidden;

            var result = await _permission.GetAll();
            return Ok(result);
        }

        [HttpGet("matrix")]
        public async Task<IActionResult> GetMatrix()
        {
            var forbidden = await RequirePermission("View");
            if (forbidden != null) return forbidden;

            var result = await _permission.GetMatrix();
            return Ok(result);
        }

        [HttpPost("set-role-permission")]
        public async Task<IActionResult> SetRolePermission([FromBody] RolePermissionSetDto dto)
        {
            if (dto == null || dto.RoleId <= 0 || dto.PermissionId <= 0 || dto.ExpectedVersion is null or < 0)
            {
                return BadRequest(new
                {
                    code = "400",
                    Success = false,
                    message = "Thông tin phân quyền không hợp lệ."
                });
            }

            var forbidden = await RequirePermission("Update");
            if (forbidden != null) return forbidden;

            var result = await _permission.SetRolePermission(dto);
            if (result.code == "409")
            {
                return Conflict(result);
            }

            if (result.Success)
            {
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            }

            return Ok(result);
        }

        [HttpGet("user-matrix")]
        public async Task<IActionResult> GetUserMatrix([FromQuery] long? userId)
        {
            var forbidden = await RequirePermission("View");
            if (forbidden != null) return forbidden;

            var result = await _permission.GetUserMatrix(userId);
            return Ok(result);
        }

        [HttpPost("set-user-permission")]
        public async Task<IActionResult> SetUserPermission([FromBody] UserPermissionSetDto dto)
        {
            Console.WriteLine($"[Server/PermissionsController] SetUserPermission received: UserId={dto?.UserId}, PermissionId={dto?.PermissionId}, IsAllowed={dto?.IsAllowed}");
            if (dto == null || dto.UserId <= 0 || dto.PermissionId <= 0)
            {
                Console.WriteLine($"[Server/PermissionsController] SetUserPermission 400 invalid: dto null={dto == null}, UserId={dto?.UserId}, PermissionId={dto?.PermissionId}");
                return BadRequest(new
                {
                    code = "400",
                    Success = false,
                    message = "Thông tin phân quyền tài khoản không hợp lệ."
                });
            }

            var forbidden = await RequirePermission("Update");
            if (forbidden != null) return forbidden;

            var result = await _permission.SetUserPermission(dto);
            if (result.Success)
            {
                await _cache.RemoveByPrefixAsync("tflix:movies:");
            }
            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyPermissions([FromQuery] string? screenCode)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "403",
                    Success = false,
                    message = "Thiếu token đăng nhập hoặc token không có email."
                });
            }

            var result = await _permission.GetUserPermissions(email, screenCode);
            return Ok(result);
        }

        [HttpPost("check")]
        public async Task<IActionResult> Check([FromBody] CheckPermissionDto dto)
        {
            var result = await _permission.CheckUserPermission(dto);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            return Ok(result);
        }

        private async Task<IActionResult?> RequirePermission(string permissionCode)
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
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "403",
                    Success = false,
                    message = "Thiếu token đăng nhập hoặc token không có email."
                });
            }

            var result = await _permission.CheckUserPermission(new CheckPermissionDto
            {
                Email = email,
                ScreenCode = "Permission",
                PermissionCode = permissionCode
            });

            if (result.Success) return null;

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "403",
                Success = false,
                message = $"Không đủ quyền {permissionCode} trên màn Permission."
            });
        }
    }
}
