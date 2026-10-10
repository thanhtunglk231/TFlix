using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/admin/account-subscriptions")]
[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public sealed class AccountSubscriptionsController : ControllerBase
{
    private readonly ICAccountSubscription _subscriptions;
    private readonly ILogger<AccountSubscriptionsController> _logger;

    public AccountSubscriptionsController(ICAccountSubscription subscriptions, ILogger<AccountSubscriptionsController> logger)
    {
        _subscriptions = subscriptions;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToActionResult(await _subscriptions.GetManagementDataAsync(), "GetAll");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountSubscriptionDto dto)
    {
        if (dto.EndAt <= dto.StartAt)
            return BadRequest(new { success = false, code = "400", message = "Thời gian kết thúc phải sau thời gian bắt đầu." });
        return ToActionResult(await _subscriptions.CreateAsync(dto), "Create");
    }

    [HttpPut("{subscriptionId:long}")]
    public async Task<IActionResult> Update(long subscriptionId, [FromBody] UpdateAccountSubscriptionDto dto)
    {
        if (subscriptionId != dto.SubscriptionId)
            return BadRequest(new { success = false, code = "400", message = "SubscriptionId không hợp lệ." });
        if (dto.EndAt <= dto.StartAt)
            return BadRequest(new { success = false, code = "400", message = "Thời gian kết thúc phải sau thời gian bắt đầu." });
        return ToActionResult(await _subscriptions.UpdateAsync(dto), "Update");
    }

    [HttpDelete("{subscriptionId:long}")]
    public async Task<IActionResult> Delete(long subscriptionId) =>
        ToActionResult(await _subscriptions.DeleteAsync(subscriptionId), "Delete");

    private IActionResult ToActionResult(CResponseMessage result, string operation)
    {
        _logger.Log(result.Success ? LogLevel.Information : LogLevel.Warning,
            "Account subscription {Operation} completed. TraceId={TraceId}, Code={Code}, Message={Message}",
            operation, HttpContext.TraceIdentifier, result.code, result.message);
        return StatusCode(result.code switch
        {
            "200" => StatusCodes.Status200OK,
            "400" => StatusCodes.Status400BadRequest,
            "404" => StatusCodes.Status404NotFound,
            "409" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        }, result);
    }
}
