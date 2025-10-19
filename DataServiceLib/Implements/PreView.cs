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
    public class PreView : IPreView
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public PreView(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }


    public CResponseMessage Get_All_Series(int seriesId)
    {
        try
        {
            using var conn = new OracleConnection(_connectionString);
            using var cmd = new OracleCommand("sp_series_preview", conn)
            {
                CommandType = CommandType.StoredProcedure,
                BindByName = true
            };

            // IN
            cmd.Parameters.Add(new OracleParameter("p_series_id", OracleDbType.Int32)
            {
                Direction = ParameterDirection.Input,
                Value = seriesId
            });

            // OUT
            cmd.Parameters.Add(new OracleParameter("o_cursor", OracleDbType.RefCursor)
            {
                Direction = ParameterDirection.Output
            });

            cmd.Parameters.Add(new OracleParameter("o_code", OracleDbType.Varchar2, 10)
            {
                Direction = ParameterDirection.Output
            });

            cmd.Parameters.Add(new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output
            });

            var ds = new DataSet();
            using (var da = new OracleDataAdapter(cmd))
            {
                conn.Open();
                da.Fill(ds);              // ds.Tables[0] = kết quả từ o_cursor
            }

            var code = cmd.Parameters["o_code"].Value?.ToString() ?? "400";
            var msg = cmd.Parameters["o_message"].Value?.ToString() ?? "Không lấy được phản hồi";

            return new CResponseMessage
            {
                Data = ds,
                code = code,
                message = msg,
                Success = code == "200"
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
    public CResponseMessage get_all(int movieId)
        {
            try
            {
                var p_movie_id= new OracleParameter("p_movie_id", OracleDbType.Int32)
                { Value = ParameterDirection.Input };
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor)
                { Direction = ParameterDirection.Output };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] {p_movie_id, o_cursor, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_preview", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
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
