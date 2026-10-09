using CoreLib.Dtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DataServiceLib.Implements.Admin
{
    public class CAdminAccount : ICAdminAccount
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;
        private readonly ILogger<CAdminAccount> _logger;

        public CAdminAccount(ICBaseProvider baseProvider, IConfiguration configuration, ILogger<CAdminAccount> logger)
        {
            _baseProvider = baseProvider;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SqlServer.");
        }

        public Task<CResponseMessage> GetManagementDataAsync()
        {
            try
            {
                _logger.LogInformation("Executing admin account management data repository flow");
                var output = CreateOutputParameters();
                var data = _baseProvider.GetDatasetFromSP(
                    "sp_admin_account_get_management_data",
                    output,
                    _connectionString);

                var response = ToResponse(data, output);
                _logger.LogInformation(
                    "Admin account management data repository flow completed with code {Code}, account rows {AccountRows}, role rows {RoleRows}",
                    response.code,
                    data.Tables.Count > 0 ? data.Tables[0].Rows.Count : 0,
                    data.Tables.Count > 1 ? data.Tables[1].Rows.Count : 0);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin account management data repository flow failed");
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> CreateAsync(CreateAdminAccountDto dto)
        {
            try
            {
                var roleIds = string.Join(',', dto.RoleIds.Distinct().OrderBy(id => id));
                _logger.LogInformation(
                    "Executing admin account create repository flow for {EmailDomain} with role ids {RoleIds}",
                    GetEmailDomain(dto.Email),
                    roleIds);
                var output = CreateOutputParameters();
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_email", SqlDbType.NVarChar, 320) { Value = dto.Email.Trim() },
                    new SqlParameter("@p_full_name", SqlDbType.NVarChar, 150) { Value = dto.FullName.Trim() },
                    new SqlParameter("@p_password", SqlDbType.NVarChar, 100) { Value = dto.Password },
                    new SqlParameter("@p_role_ids", SqlDbType.NVarChar, 2000) { Value = roleIds },
                    output[0],
                    output[1]
                };

                var data = _baseProvider.GetDatasetFromSP("sp_admin_account_create", parameters, _connectionString);
                var response = ToResponse(data, output);
                _logger.LogInformation("Admin account create repository flow completed with code {Code} and success {Success}", response.code, response.Success);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin account create repository flow failed for {EmailDomain}", GetEmailDomain(dto.Email));
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> UpdateAsync(UpdateAdminAccountDto dto)
        {
            try
            {
                var roleIds = string.Join(',', dto.RoleIds.Distinct().OrderBy(id => id));
                _logger.LogInformation(
                    "Executing admin account update repository flow for user {UserId} ({EmailDomain}) with role ids {RoleIds}",
                    dto.UserId,
                    GetEmailDomain(dto.Email),
                    roleIds);
                var output = CreateOutputParameters();
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = dto.UserId },
                    new SqlParameter("@p_email", SqlDbType.NVarChar, 320) { Value = dto.Email.Trim() },
                    new SqlParameter("@p_full_name", SqlDbType.NVarChar, 200) { Value = dto.FullName.Trim() },
                    new SqlParameter("@p_password", SqlDbType.NVarChar, 500) { Value = string.IsNullOrWhiteSpace(dto.Password) ? (object)DBNull.Value : dto.Password },
                    new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(dto.Status) ? "ACTIVE" : dto.Status.Trim() },
                    new SqlParameter("@p_role_ids", SqlDbType.NVarChar, -1) { Value = roleIds },
                    output[0],
                    output[1]
                };

                var data = _baseProvider.GetDatasetFromSP("usp_AdminAccount_Update", parameters, _connectionString);
                var response = ToResponse(data, output);
                _logger.LogInformation("Admin account update repository flow completed with code {Code} and success {Success}", response.code, response.Success);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin account update repository flow failed for user {UserId}", dto.UserId);
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> DeleteAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Executing admin account delete repository flow for user {UserId}", userId);
                var output = CreateOutputParameters();
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = userId },
                    output[0],
                    output[1]
                };

                var data = _baseProvider.GetDatasetFromSP("usp_AdminAccount_Delete", parameters, _connectionString);
                var response = ToResponse(data, output);
                _logger.LogInformation("Admin account delete repository flow completed with code {Code} and success {Success}", response.code, response.Success);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin account delete repository flow failed for user {UserId}", userId);
                return Task.FromResult(Error(ex));
            }
        }

        private static string GetEmailDomain(string email) =>
            email.Contains('@') ? email[(email.IndexOf('@') + 1)..] : "invalid";

        private static IDbDataParameter[] CreateOutputParameters()
        {
            return new IDbDataParameter[]
            {
                new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output },
                new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output }
            };
        }

        private static CResponseMessage ToResponse(DataSet data, IDbDataParameter[] output)
        {
            var code = output[0].Value?.ToString() ?? "500";
            return new CResponseMessage
            {
                Data = data,
                code = code,
                message = output[1].Value?.ToString() ?? "Không lấy được phản hồi.",
                Success = code == "200"
            };
        }

        private static CResponseMessage Error(Exception ex) => new()
        {
            Success = false,
            code = "500",
            message = "Lỗi quản lý tài khoản: " + ex.Message
        };
    }
}
