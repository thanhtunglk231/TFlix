using CoreLib.Dtos.Subscription;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers;

public sealed class SubscriptionPlanController : AdminBaseController
{
    private readonly ISubscriptionPlanService _service;

    public SubscriptionPlanController(ISubscriptionPlanService service) => _service = service;

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto) =>
        Ok(await _service.CreateAsync(dto));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateSubscriptionPlanDto dto) =>
        Ok(await _service.UpdateAsync(dto));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeletePlanRequest request) =>
        Ok(await _service.DeleteAsync(request.PlanId));

    public sealed class DeletePlanRequest
    {
        public long PlanId { get; set; }
    }
}
