using System.Data;
using System.Diagnostics;
using CoreLib.Dtos.Subscription;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataServiceLib.Implements.Admin;

public sealed class CAccountSubscription : ICAccountSubscription
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;
    private readonly ILogger<CAccountSubscription> _logger;

    public CAccountSubscription(ICBaseProvider baseProvider, IConfiguration configuration, ILogger<CAccountSubscription> logger)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing SqlServer connection string.");
        _logger = logger;
    }

    public Task<CResponseMessage> GetManagementDataAsync() => ExecuteAsync("sp_account_subscription_get_management_data", []);

    public Task<CResponseMessage> CreateAsync(CreateAccountSubscriptionDto dto) =>
        SaveAsync("sp_account_subscription_create", dto, null);

    public Task<CResponseMessage> UpdateAsync(UpdateAccountSubscriptionDto dto) =>
        SaveAsync("sp_account_subscription_update", dto, dto.SubscriptionId);

    public Task<CResponseMessage> DeleteAsync(long subscriptionId) => ExecuteAsync(
        "sp_account_subscription_delete",
        [new SqlParameter("@p_subscription_id", SqlDbType.BigInt) { Value = subscriptionId }]);

    private Task<CResponseMessage> SaveAsync(string procedure, SaveAccountSubscriptionDto dto, long? subscriptionId)
    {
        var parameters = new List<IDbDataParameter>();
        if (subscriptionId.HasValue)
            parameters.Add(new SqlParameter("@p_subscription_id", SqlDbType.BigInt) { Value = subscriptionId.Value });

        parameters.AddRange([
            new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = dto.UserId },
            new SqlParameter("@p_plan_id", SqlDbType.BigInt) { Value = dto.PlanId },
            new SqlParameter("@p_start_at", SqlDbType.DateTimeOffset) { Value = dto.StartAt },
            new SqlParameter("@p_end_at", SqlDbType.DateTimeOffset) { Value = dto.EndAt },
            new SqlParameter("@p_status", SqlDbType.NVarChar, 20) { Value = dto.Status.Trim().ToUpperInvariant() }
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
            var data = _baseProvider.GetDatasetFromSP(procedure, input.Concat([code, message]).ToArray(), _connectionString);
            var resultCode = code.Value?.ToString() ?? "500";
            var result = new CResponseMessage
            {
                Success = resultCode == "200",
                code = resultCode,
                message = message.Value?.ToString() ?? "Không xử lý được đăng ký Premium.",
                Data = data
            };

            _logger.Log(result.Success ? LogLevel.Information : LogLevel.Warning,
                "Account subscription procedure {Procedure} returned {ResultCode} in {ElapsedMilliseconds} ms: {ResultMessage}",
                procedure, result.code, stopwatch.ElapsedMilliseconds, result.message);
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Account subscription procedure {Procedure} failed", procedure);
            return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Không xử lý được đăng ký Premium." });
        }
    }
}
