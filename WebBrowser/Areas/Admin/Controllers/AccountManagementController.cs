using CoreLib.Dtos;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class AccountManagementController : AdminBaseController
    {
        private readonly IAdminAccountService _adminAccountService;
        private readonly ILogger<AccountManagementController> _logger;

        public AccountManagementController(IAdminAccountService adminAccountService, ILogger<AccountManagementController> logger)
        {
            _adminAccountService = adminAccountService;
            _logger = logger;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ManagementData()
        {
            _logger.LogInformation("Admin account management data request started");
            var result = await _adminAccountService.GetManagementDataAsync();
            _logger.LogInformation("Admin account management data request completed with code {Code} and success {Success}", result.code, result.Success);
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateAdminAccountDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Success = false, code = "400", message = "Vui lòng kiểm tra lại thông tin tài khoản." });
            }

            _logger.LogInformation("Admin account create request started for {EmailDomain} with {RoleCount} roles", GetEmailDomain(dto.Email), dto.RoleIds.Count);
            var result = await _adminAccountService.CreateAsync(dto);
            _logger.LogInformation("Admin account create request completed with code {Code} and success {Success}", result.code, result.Success);
            return Ok(result);
        }

        private static string GetEmailDomain(string email) =>
            email.Contains('@') ? email[(email.IndexOf('@') + 1)..] : "invalid";
    }
}
