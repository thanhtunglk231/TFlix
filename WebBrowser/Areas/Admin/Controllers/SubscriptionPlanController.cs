using CoreLib.Models;
using CoreLib.Dtos.Subscription;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers;

public sealed class SubscriptionPlanController : AdminBaseController
{
    private readonly ISubscriptionPlanService _service;
    private readonly ILogger<SubscriptionPlanController> _logger;

    public SubscriptionPlanController(
        ISubscriptionPlanService service,
        ILogger<SubscriptionPlanController> logger)
    {
        _service = service;
        _logger = logger;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        LogResult("GetAll", null, null, result);
        return new ContentResult
        {
            Content = JsonConvert.SerializeObject(result),
            ContentType = "application/json; charset=utf-8",
            StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        LogResult("Create", null, dto?.PlanCode, result);
        return ToActionResult(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateSubscriptionPlanDto dto)
    {
        var result = await _service.UpdateAsync(dto);
        LogResult("Update", dto?.PlanId, dto?.PlanCode, result);
        return ToActionResult(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeletePlanRequest request)
    {
        var result = await _service.DeleteAsync(request.PlanId);
        LogResult("Delete", request.PlanId, null, result);
        return ToActionResult(result);
    }

    private void LogResult(string operation, long? planId, string? planCode, CResponseMessage response)
    {
        var message = "Subscription plan MVC operation {Operation} completed. TraceId={TraceId}, PlanId={PlanId}, PlanCode={PlanCode}, ResultCode={ResultCode}, Message={ResultMessage}";
        var values = new object?[]
        {
            operation,
            HttpContext.TraceIdentifier,
            planId,
            planCode,
            response.code,
            response.message
        };

        if (response.Success)
            _logger.LogInformation(message, values);
        else
            _logger.LogWarning(message, values);
    }

    private IActionResult ToActionResult(CResponseMessage response)
    {
        if (response.Success)
            return Ok(response);

        var statusCode = response.code switch
        {
            "400" => StatusCodes.Status400BadRequest,
            "401" => StatusCodes.Status401Unauthorized,
            "403" => StatusCodes.Status403Forbidden,
            "404" => StatusCodes.Status404NotFound,
            "409" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, response);
    }

    public sealed class DeletePlanRequest
    {
        public long PlanId { get; set; }
    }
}
