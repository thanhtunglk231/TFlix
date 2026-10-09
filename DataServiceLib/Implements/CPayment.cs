using System.Data;
using CoreLib.Dtos.Payment;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataServiceLib.Implements;

public sealed class CPayment : ICPayment
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;

    public CPayment(ICBaseProvider baseProvider, IConfiguration configuration)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing SqlServer connection string.");
    }

    public Task<CResponseMessage> GetPlansAsync() => ExecuteAsync("sp_payment_plan_get_all", []);

    public Task<CResponseMessage> GetSubscriptionStatusAsync(string email) => ExecuteAsync(
        "sp_payment_subscription_status",
        [new SqlParameter("@p_email", SqlDbType.NVarChar, 255) { Value = email }]);

    public Task<CResponseMessage> CreatePendingAsync(string email, long planId, long orderCode) => ExecuteAsync(
        "sp_payment_create_pending",
        [
            new SqlParameter("@p_email", SqlDbType.NVarChar, 255) { Value = email },
            new SqlParameter("@p_plan_id", SqlDbType.BigInt) { Value = planId },
            new SqlParameter("@p_order_code", SqlDbType.BigInt) { Value = orderCode }
        ]);

    public Task<CResponseMessage> CompletePayOsAsync(long orderCode, decimal amount) => ExecuteAsync(
        "sp_payment_payos_complete",
        [new SqlParameter("@p_order_code", SqlDbType.BigInt) { Value = orderCode }, new SqlParameter("@p_amount", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = amount }]);

    public Task<CResponseMessage> FailPayOsAsync(long orderCode) => ExecuteAsync(
        "sp_payment_payos_fail",
        [new SqlParameter("@p_order_code", SqlDbType.BigInt) { Value = orderCode }]);

    private Task<CResponseMessage> ExecuteAsync(string procedure, IEnumerable<IDbDataParameter> input)
    {
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var data = _baseProvider.GetDatasetFromSP(procedure, input.Concat([code, message]).ToArray(), _connectionString);
            var resultCode = code.Value?.ToString() ?? "500";
            return Task.FromResult(new CResponseMessage
            {
                Success = resultCode == "200",
                code = resultCode,
                message = message.Value?.ToString() ?? "Không thể xử lý thanh toán.",
                Data = data
            });
        }
        catch (Exception)
        {
            return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Không thể xử lý thanh toán." });
        }
    }
}

