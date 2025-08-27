using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CAuth : ICAuth
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CAuth(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }


        public async Task<CResponseMessage> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var p_email = new OracleParameter("p_email", OracleDbType.Varchar2)
                {
                    Direction = ParameterDirection.Input,
                    Value = loginDto.Username
                };
                var p_password = new OracleParameter("p_password", OracleDbType.Varchar2)
                {
                    Direction = ParameterDirection.Input,
                    Value = loginDto.Password
                };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };
                var o_user = new OracleParameter("o_user", OracleDbType.RefCursor)
                {
                    Direction = ParameterDirection.Output
                };

                // THỨ TỰ phải khớp: p_email, p_password, o_code, o_message, o_user
                var parameters = new OracleParameter[] { p_email, p_password, o_code, o_message, o_user };

                var dataset = _baseProvider.GetDatasetFromSP("sp_login_user", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,                         // DataSet có Tables[0] khi login thành công
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }


        public async Task<CResponseMessage> Register(RegisterDto loginDto)
        {
            try
            {
                var p_email = new OracleParameter("p_email", OracleDbType.Varchar2)
                {
                    Direction = ParameterDirection.Input,
                    Value = loginDto.Email
                };
                var p_fullname = new OracleParameter("p_full_name", OracleDbType.Varchar2)
                {
                    Direction = ParameterDirection.Input,
                    Value = loginDto.FullName   
                };
                var p_password = new OracleParameter("p_password", OracleDbType.Varchar2)
                {
                    Direction = ParameterDirection.Input,
                    Value = loginDto.Password
                };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                // Thứ tự phải khớp với SP: p_email, p_full_name, p_password, o_code, o_message
                var parameters = new OracleParameter[] { p_email, p_fullname, p_password, o_code, o_message };

                var ds = _baseProvider.GetDatasetFromSP("sp_register_user", parameters, _connectionString);
                // SP không mở cursor => ds có thể rỗng, không sao.

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
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
}





