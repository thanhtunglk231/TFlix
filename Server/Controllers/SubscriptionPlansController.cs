using CoreLib.Dtos.Subscription;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/admin/subscription-plans")]
[Authorize(Roles = "ADMIN")]
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
        return StatusCode(result.Success ? 200 : 500, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto)
    {
        var result = await _plans.CreateAsync(dto);
        _logger.LogInformation("Admin created subscription plan {PlanCode}: {ResultCode}", dto.PlanCode, result.code);
        return StatusCode(ToStatusCode(result.code), result);
    }

    [HttpPut("{planId:long}")]
    public async Task<IActionResult> Update(long planId, [FromBody] UpdateSubscriptionPlanDto dto)
    {
        if (planId != dto.PlanId)
            return BadRequest(new { success = false, code = "400", message = "PlanId không hợp lệ." });
        var result = await _plans.UpdateAsync(dto);
        _logger.LogInformation("Admin updated subscription plan {PlanId}: {ResultCode}", planId, result.code);
        return StatusCode(ToStatusCode(result.code), result);
    }

    [HttpDelete("{planId:long}")]
    public async Task<IActionResult> Delete(long planId)
    {
        var result = await _plans.DeleteAsync(planId);
        _logger.LogInformation("Admin deleted subscription plan {PlanId}: {ResultCode}", planId, result.code);
        return StatusCode(ToStatusCode(result.code), result);
    }

    private static int ToStatusCode(string? code) => code switch
    {
        "200" => StatusCodes.Status200OK,
        "404" => StatusCodes.Status404NotFound,
        "409" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
