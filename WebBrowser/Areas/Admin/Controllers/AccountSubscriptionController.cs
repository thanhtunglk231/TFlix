using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers;

public sealed class AccountSubscriptionController : AdminBaseController
{
    private readonly IAccountSubscriptionService _service;

    public AccountSubscriptionController(IAccountSubscriptionService service) => _service = service;

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _service.GetManagementDataAsync();
        if (!response.Success)
            return ToActionResult(response);

        var dataSet = response.Data is null
            ? new DataSet()
            : JsonConvert.DeserializeObject<DataSet>(JsonConvert.SerializeObject(response.Data)) ?? new DataSet();

        return Ok(new
        {
            success = true,
            code = response.code,
            message = response.message,
            subscriptions = Rows(dataSet, 0),
            users = Rows(dataSet, 1),
            plans = Rows(dataSet, 2)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateAccountSubscriptionDto dto)
    {
        if (!ModelState.IsValid || dto.EndAt <= dto.StartAt)
            return BadRequest(new { success = false, code = "400", message = "Dữ liệu đăng ký Premium không hợp lệ." });
        return ToActionResult(await _service.CreateAsync(dto));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateAccountSubscriptionDto dto)
    {
        if (!ModelState.IsValid || dto.EndAt <= dto.StartAt)
            return BadRequest(new { success = false, code = "400", message = "Dữ liệu đăng ký Premium không hợp lệ." });
        return ToActionResult(await _service.UpdateAsync(dto));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeleteSubscriptionRequest request) =>
        ToActionResult(await _service.DeleteAsync(request.SubscriptionId));

    private static IEnumerable<Dictionary<string, object?>> Rows(DataSet dataSet, int tableIndex)
    {
        if (dataSet.Tables.Count <= tableIndex)
            return [];

        return dataSet.Tables[tableIndex].AsEnumerable().Select(row =>
            row.Table.Columns.Cast<DataColumn>().ToDictionary(
                column => column.ColumnName,
                column => row[column] == DBNull.Value ? null : row[column]));
    }

    private IActionResult ToActionResult(CResponseMessage response) => response.Success
        ? Ok(response)
        : StatusCode(response.code switch
        {
            "400" => 400,
            "404" => 404,
            "409" => 409,
            _ => 500
        }, response);

    public sealed class DeleteSubscriptionRequest
    {
        public long SubscriptionId { get; set; }
    }
}
