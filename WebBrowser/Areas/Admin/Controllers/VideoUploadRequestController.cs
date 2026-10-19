using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers;

public class VideoUploadRequestController : AdminBaseController
{
    private readonly IVideoUploadRequestService _service;

    public VideoUploadRequestController(IVideoUploadRequestService service)
    {
        _service = service;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? status = null)
    {
        var data = await _service.GetAllAsync(status);
        return Ok(new
        {
            success = true,
            data
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review([FromBody] ReviewVideoUploadRequestDto dto)
    {
        if (dto == null || dto.RequestId <= 0)
        {
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        if (dto.Status != "APPROVED" && dto.Status != "REJECTED")
        {
            return BadRequest(new { success = false, message = "Trạng thái phê duyệt không hợp lệ. Chỉ chấp nhận APPROVED hoặc REJECTED." });
        }

        dto.AdminUserId = GetCurrentAdminUserId();
        dto.ReviewedBy = dto.AdminUserId;

        var result = await _service.ReviewAsync(dto);
        if (result == null)
        {
            return StatusCode(500, new { success = false, message = "Không thể kết nối đến máy chủ." });
        }

        return Ok(result);
    }
}
