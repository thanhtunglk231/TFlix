using CoreLib.Dtos.Preview;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
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
            _connectionString = configuration.GetConnectionString("SqlServer");
        }


    public CResponseMessage Get_All_Series(int seriesId)
    {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_series_preview", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // IN
                cmd.Parameters.Add(new SqlParameter("@p_series_id", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = seriesId
                });

                // Output parameters for code/message
                cmd.Parameters.Add(new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                });

                cmd.Parameters.Add(new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                });

                var ds = new DataSet();
                using (var da = new SqlDataAdapter(cmd))
                {
                    conn.Open();
                    da.Fill(ds); // first resultset is the SP result
                }

                var code = cmd.Parameters["@o_code"].Value?.ToString() ?? "400";
                var msg = cmd.Parameters["@o_message"].Value?.ToString() ?? "Không lấy được phản hồi";

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
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = movieId };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, o_code, o_message };

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

        public CResponseMessage GET_CONTENT_BY_ID(GETCONTENTByID movie)
        {
            try
            {
                Console.WriteLine("========== [SP_GET_CONTENT_BY_ID] CALL ==========");
                Console.WriteLine($"Input movie.id   = {movie?.id}");
                Console.WriteLine($"Input movie.kind = {movie?.kind}");
                Console.WriteLine("=================================================");

                var p_movie_id = new SqlParameter("@p_id", SqlDbType.Int) { Value = movie.id };
                var p_kind = new SqlParameter("@p_kind", SqlDbType.NVarChar, 50) { Value = (object?)movie.kind ?? DBNull.Value };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, p_kind, o_code, o_message };

               
                // Gọi SP (SQL Server stored procedure should return a resultset directly)
                var dataset = _baseProvider.GetDatasetFromSP("SP_GET_CONTENT_BY_ID", parameters, _connectionString);

                // Debug dataset return
                if (dataset != null)
                {
                    Console.WriteLine($">>> Dataset Returned: Tables = {dataset.Tables.Count}");
                    for (int i = 0; i < dataset.Tables.Count; i++)
                    {
                        var tbl = dataset.Tables[i];
                        Console.WriteLine($"    Table[{i}] Name: {tbl.TableName} Rows: {tbl.Rows.Count} Cols: {tbl.Columns.Count}");
                    }
                }
                else
                {
                    Console.WriteLine(">>> Dataset = NULL");
                }

                // Đọc output của SP
                string outCode = o_code.Value?.ToString();
                string outMessage = o_message.Value?.ToString();

                Console.WriteLine(">>> SP Output:");
                Console.WriteLine($" - o_code    = {outCode}");
                Console.WriteLine($" - o_message = {outMessage}");
                Console.WriteLine("=================================================");

                return new CResponseMessage
                {
                    Data = dataset,
                    code = outCode ?? "400",
                    message = outMessage ?? "Không lấy được phản hồi",
                    Success = outCode == "200"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("=========== [SP_GET_CONTENT_BY_ID] EXCEPTION ===========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================================================");

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
