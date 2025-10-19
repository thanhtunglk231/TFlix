using CoreLib.Dtos.Home;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CHome : ICHome
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CHome(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }


        public async Task<CResponseMessage> MovieLastestItem()
        {
            try
            {
                // NÊN: nếu _baseProvider không tự set BindByName=true thì bạn phải giữ đúng thứ tự tham số như SP:
                // (p_limit IN, o_cursor OUT, o_code OUT, o_message OUT)

                var p_limit = new OracleParameter("p_limit", OracleDbType.Int32)
                {
                    Direction = ParameterDirection.Input,
                    Value = 5 // hoặc truyền từ ngoài; NULL không dùng mặc định được ở client driver
                };

                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor)
                {
                    Direction = ParameterDirection.Output
                };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

        
                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_movies_get_latest",
                    new[] { p_limit, o_cursor, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = string.Equals(o_code.Value?.ToString(), "200", StringComparison.Ordinal)
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
