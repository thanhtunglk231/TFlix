using CoreLib.Dtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements.Admin
{
    public class CPermission : ICPermission
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CPermission(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public Task<CResponseMessage> GetAll()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_permission_get_all", parameters, _connectionString);

                return Task.FromResult(new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> GetMatrix()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_permission_matrix_get_all", parameters, _connectionString);

                return Task.FromResult(new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> SetRolePermission(RolePermissionSetDto dto)
        {
            try
            {
                var p_role_id = new SqlParameter("@p_role_id", SqlDbType.BigInt) { Direction = ParameterDirection.Input, Value = dto.RoleId };
                var p_permission_id = new SqlParameter("@p_permission_id", SqlDbType.BigInt) { Direction = ParameterDirection.Input, Value = dto.PermissionId };
                var p_is_allowed = new SqlParameter("@p_is_allowed", SqlDbType.Bit) { Direction = ParameterDirection.Input, Value = dto.IsAllowed };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { p_role_id, p_permission_id, p_is_allowed, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_role_permission_set", parameters, _connectionString);

                return Task.FromResult(new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> GetUserPermissions(string email, string? screenCode)
        {
            try
            {
                var p_email = new SqlParameter("@p_email", SqlDbType.NVarChar, 320) { Direction = ParameterDirection.Input, Value = email };
                var p_screen_code = new SqlParameter("@p_screen_code", SqlDbType.NVarChar, 100)
                {
                    Direction = ParameterDirection.Input,
                    Value = string.IsNullOrWhiteSpace(screenCode) ? DBNull.Value : screenCode
                };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { p_email, p_screen_code, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_permission_get_by_user", parameters, _connectionString);

                return Task.FromResult(new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(Error(ex));
            }
        }

        public Task<CResponseMessage> CheckUserPermission(CheckPermissionDto dto)
        {
            try
            {
                var p_email = new SqlParameter("@p_email", SqlDbType.NVarChar, 320) { Direction = ParameterDirection.Input, Value = dto.Email };
                var p_screen_code = new SqlParameter("@p_screen_code", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Input, Value = dto.ScreenCode };
                var p_permission_code = new SqlParameter("@p_permission_code", SqlDbType.NVarChar, 20) { Direction = ParameterDirection.Input, Value = dto.PermissionCode };
                var o_allowed = new SqlParameter("@o_allowed", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { p_email, p_screen_code, p_permission_code, o_allowed, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_permission_check_user", parameters, _connectionString);
                var allowed = o_allowed.Value != DBNull.Value && Convert.ToBoolean(o_allowed.Value);

                return Task.FromResult(new CResponseMessage
                {
                    Data = new { Allowed = allowed, DataSet = dataset },
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = allowed && o_code.Value?.ToString() == "200"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(Error(ex));
            }
        }

        private static CResponseMessage Error(Exception ex)
        {
            return new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Lỗi server: " + ex.Message
            };
        }
    }
}
