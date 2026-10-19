using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoUploadRequestsController : ControllerBase
{
    private readonly ICVideoUploadRequest _service;
    private readonly ILogger<VideoUploadRequestsController> _logger;

    public VideoUploadRequestsController(ICVideoUploadRequest service, ILogger<VideoUploadRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] CreateVideoUploadRequestDto dto)
    {
        if (dto == null || dto.UserId <= 0)
        {
            return BadRequest(new CResponseMessage
            {
                Success = false,
                code = "400",
                message = "Thông tin yêu cầu không hợp lệ."
            });
        }

        var result = await _service.SubmitRequestAsync(dto);
        return ToActionResult(result, "Submit");
    }

    [HttpGet("status/{userId:long}")]
    public async Task<IActionResult> GetStatus(long userId)
    {
        if (userId <= 0)
        {
            return BadRequest(new CResponseMessage
            {
                Success = false,
                code = "400",
                message = "UserId không hợp lệ."
            });
        }

        var result = await _service.GetUserStatusAsync(userId);
        return ToActionResult(result, "GetStatus");
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var result = await _service.GetAllAsync(status);
        return ToActionResult(result, "GetAll");
    }

    [HttpPost("review")]
    public async Task<IActionResult> Review([FromBody] ReviewVideoUploadRequestDto dto)
    {
        if (dto == null || dto.RequestId <= 0 || string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new CResponseMessage
            {
                Success = false,
                code = "400",
                message = "Dữ liệu phê duyệt không hợp lệ."
            });
        }

        var result = await _service.ReviewRequestAsync(dto);
        return ToActionResult(result, "Review");
    }

    private IActionResult ToActionResult(CResponseMessage result, string operation)
    {
        _logger.Log(result.Success ? LogLevel.Information : LogLevel.Warning,
            "Video upload request {Operation} completed. Code={Code}, Message={Message}",
            operation, result.code, result.message);

        return StatusCode(result.code switch
        {
            "200" => StatusCodes.Status200OK,
            "400" => StatusCodes.Status400BadRequest,
            "404" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        }, result);
    }
}
