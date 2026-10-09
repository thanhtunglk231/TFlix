using System.Data;
using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DataServiceLib.Implements.Admin;

public sealed class CSubscriptionPlan : ICSubscriptionPlan
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;
    private readonly ILogger<CSubscriptionPlan> _logger;

    public CSubscriptionPlan(ICBaseProvider baseProvider, IConfiguration configuration, ILogger<CSubscriptionPlan> logger)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing SqlServer connection string.");
        _logger = logger;
    }

    public Task<CResponseMessage> GetAllAsync() => ExecuteAsync("sp_subscription_plan_get_all", []);

    public Task<CResponseMessage> CreateAsync(CreateSubscriptionPlanDto dto) =>
        SaveAsync("sp_subscription_plan_create", dto, null);

    public Task<CResponseMessage> UpdateAsync(UpdateSubscriptionPlanDto dto) =>
        SaveAsync("sp_subscription_plan_update", dto, dto.PlanId);

    public Task<CResponseMessage> DeleteAsync(long planId) => ExecuteAsync(
        "sp_subscription_plan_delete",
        [new SqlParameter("@p_plan_id", SqlDbType.BigInt) { Value = planId }]);

    private Task<CResponseMessage> SaveAsync(string procedure, SubscriptionPlanDto dto, long? planId)
    {
        var parameters = new List<IDbDataParameter>();
        if (planId.HasValue)
            parameters.Add(new SqlParameter("@p_plan_id", SqlDbType.BigInt) { Value = planId.Value });
        parameters.AddRange([
            new SqlParameter("@p_plan_code", SqlDbType.NVarChar, 50) { Value = dto.PlanCode.Trim().ToUpperInvariant() },
            new SqlParameter("@p_name", SqlDbType.NVarChar, 100) { Value = dto.Name.Trim() },
            new SqlParameter("@p_price", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = dto.Price },
            new SqlParameter("@p_duration_days", SqlDbType.Int) { Value = dto.DurationDays },
            new SqlParameter("@p_max_devices", SqlDbType.Int) { Value = dto.MaxDevices },
            new SqlParameter("@p_quality_cap", SqlDbType.NVarChar, 20) { Value = (object?)dto.QualityCap?.Trim() ?? DBNull.Value },
            new SqlParameter("@p_ads_free", SqlDbType.Char, 1) { Value = dto.AdsFree ? "Y" : "N" },
            new SqlParameter("@p_downloadable", SqlDbType.Char, 1) { Value = dto.Downloadable ? "Y" : "N" }
        ]);
        return ExecuteAsync(procedure, parameters);
    }

    private Task<CResponseMessage> ExecuteAsync(string procedure, IEnumerable<IDbDataParameter> input)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var parameters = input.Concat([code, message]).ToArray();
            var data = _baseProvider.GetDatasetFromSP(procedure, parameters, _connectionString);
            var resultCode = code.Value?.ToString() ?? "500";
            var result = new CResponseMessage
            {
                Success = resultCode == "200",
                code = resultCode,
                message = message.Value?.ToString() ?? "Không xử lý được gói Premium.",
                Data = data
            };

            if (result.Success)
            {
                _logger.LogInformation(
                    "Subscription plan stored procedure {Procedure} returned {ResultCode} in {ElapsedMilliseconds} ms with {RowCount} rows",
                    procedure,
                    result.code,
                    stopwatch.ElapsedMilliseconds,
                    data.Tables.Count > 0 ? data.Tables[0].Rows.Count : 0);
            }
            else
            {
                _logger.LogWarning(
                    "Subscription plan stored procedure {Procedure} returned {ResultCode} in {ElapsedMilliseconds} ms: {ResultMessage}",
                    procedure,
                    result.code,
                    stopwatch.ElapsedMilliseconds,
                    result.message);
            }

            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Stored procedure {Procedure} failed while managing a subscription plan", procedure);
            return Task.FromResult(new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Không xử lý được gói Premium."
            });
        }
    }
}
