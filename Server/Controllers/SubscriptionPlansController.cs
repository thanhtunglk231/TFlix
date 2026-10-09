using CoreLib.Dtos.Subscription;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/admin/subscription-plans")]
[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly ICSubscriptionPlan _plans;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(ICSubscriptionPlan plans, ILogger<SubscriptionPlansController> logger)
    {
        _plans = plans;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _plans.GetAllAsync();
        LogResult("GetAll", null, null, result);
        return StatusCode(result.Success ? 200 : 500, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto)
    {
        var result = await _plans.CreateAsync(dto);
        LogResult("Create", null, dto.PlanCode, result);
        return StatusCode(ToStatusCode(result.code), result);
    }

    [HttpPut("{planId:long}")]
    public async Task<IActionResult> Update(long planId, [FromBody] UpdateSubscriptionPlanDto dto)
    {
        if (planId != dto.PlanId)
            return BadRequest(new { success = false, code = "400", message = "PlanId không hợp lệ." });
        var result = await _plans.UpdateAsync(dto);
        LogResult("Update", planId, dto.PlanCode, result);
        return StatusCode(ToStatusCode(result.code), result);
    }

    [HttpDelete("{planId:long}")]
    public async Task<IActionResult> Delete(long planId)
    {
        var result = await _plans.DeleteAsync(planId);
        LogResult("Delete", planId, null, result);
        return StatusCode(ToStatusCode(result.code), result);
    }

    private void LogResult(string operation, long? planId, string? planCode, CoreLib.Models.CResponseMessage result)
    {
        var message = "Subscription plan operation {Operation} completed. TraceId={TraceId}, PlanId={PlanId}, PlanCode={PlanCode}, ResultCode={ResultCode}, Message={ResultMessage}";
        var values = new object?[]
        {
            operation,
            HttpContext.TraceIdentifier,
            planId,
            planCode,
            result.code,
            result.message
        };

        if (result.Success)
            _logger.LogInformation(message, values);
        else
            _logger.LogWarning(message, values);
    }

    private static int ToStatusCode(string? code) => code switch
    {
        "200" => StatusCodes.Status200OK,
        "400" => StatusCodes.Status400BadRequest,
        "401" => StatusCodes.Status401Unauthorized,
        "403" => StatusCodes.Status403Forbidden,
        "404" => StatusCodes.Status404NotFound,
        "409" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
