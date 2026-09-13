using CoreLib.Dtos;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly ICPermission _permission;

        public PermissionsController(ICPermission permission)
        {
            _permission = permission;
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
            var forbidden = await RequirePermission("Update");
            if (forbidden != null) return forbidden;

            var result = await _permission.SetRolePermission(dto);
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
