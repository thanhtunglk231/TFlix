using CoreLib.Dtos.Home;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
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
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> MovieLastestItem()
        {
            try
            {
                var p_limit = new SqlParameter("@p_limit", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = 5
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_limit, o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_movies_get_latest",
                    parameters,
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