using CoreLib.Dtos.AuthDtos;
using CoreLib.Dtos.Movies;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovie : ICMovie
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovie(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }


        public async Task<CResponseMessage> get_all()
        {
            try
            {


                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor)
                {
                    Direction = ParameterDirection.Output
                };

                
                var parameters = new OracleParameter[] { o_cursor, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_movie", parameters, _connectionString);

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
        public async Task<CResponseMessage> Add_movie(AddMovieDto addMovieDto)
        {
            try
            {
                // IN parameters
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, addMovieDto.Title, ParameterDirection.Input);
                var p_release_date = new OracleParameter("p_release_date", OracleDbType.Date, (object?)addMovieDto.ReleaseDate ?? DBNull.Value, ParameterDirection.Input);
                var p_duration_min = new OracleParameter("p_duration_min", OracleDbType.Int32, (object?)addMovieDto.DurationMin ?? DBNull.Value, ParameterDirection.Input);
                var p_language_code = new OracleParameter("p_language_code", OracleDbType.Varchar2, (object?)addMovieDto.LanguageCode ?? DBNull.Value, ParameterDirection.Input);
                var p_country_code = new OracleParameter("p_country_code", OracleDbType.Varchar2, (object?)addMovieDto.CountryCode ?? DBNull.Value, ParameterDirection.Input);
                // ✅ CHAR(1) cho Y/N
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, 1, addMovieDto.IsPremium ? "Y" : "N", ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)addMovieDto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, (object?)addMovieDto.Status ?? "PUBLISHED", ParameterDirection.Input);

                // OUT parameters
                var o_movie_id = new OracleParameter("o_movie_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
            p_title, p_release_date, p_duration_min,
            p_language_code, p_country_code, p_is_premium,
            p_overview, p_status,
            o_movie_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_add", parameters, _connectionString);

                // ✅ ĐỌC OUT NUMBER ĐÚNG CÁCH
                decimal? newId = null;
                if (o_movie_id.Value != null && o_movie_id.Value != DBNull.Value)
                {
                    var od = (OracleDecimal)o_movie_id.Value;
                    if (!od.IsNull) newId = od.Value;
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, MovieId = newId },
                    code = o_code.Value?.ToString() ?? "500",
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

        public async Task<CResponseMessage> Update_movie(UpdateMovieDto updateMovieDto)
        {
            try
            {
                // IN parameters
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, updateMovieDto.Movideid, ParameterDirection.Input);
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, updateMovieDto.Title, ParameterDirection.Input);
                var p_release_date = new OracleParameter("p_release_date", OracleDbType.Date, (object?)updateMovieDto.ReleaseDate ?? DBNull.Value, ParameterDirection.Input);
                var p_duration_min = new OracleParameter("p_duration_min", OracleDbType.Int32, (object?)updateMovieDto.DurationMin ?? DBNull.Value, ParameterDirection.Input);
                var p_language_code = new OracleParameter("p_language_code", OracleDbType.Varchar2, (object?)updateMovieDto.LanguageCode ?? DBNull.Value, ParameterDirection.Input);
                var p_country_code = new OracleParameter("p_country_code", OracleDbType.Varchar2, (object?)updateMovieDto.CountryCode ?? DBNull.Value, ParameterDirection.Input);
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, updateMovieDto.IsPremium ? "Y" : "N", ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)updateMovieDto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, (object?)updateMovieDto.Status ?? "PUBLISHED", ParameterDirection.Input);

                // OUT parameters
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new OracleParameter[]
                {
            p_movie_id, p_title, p_release_date, p_duration_min,
            p_language_code, p_country_code, p_is_premium,
            p_overview, p_status,
            o_code, o_message
                };

                // Gọi stored procedure
                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // có thể null vì SP này không mở cursor
                    code = o_code.Value?.ToString() ?? "500",
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


        public async Task<CResponseMessage> Delete_movie(decimal movieId)
        {
            try
            {
                // IN parameter
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);

                // OUT parameters
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new OracleParameter[]
                {
            p_movie_id, o_code, o_message
                };

                // Gọi stored procedure
                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // SP này không có cursor nên dataset có thể rỗng
                    code = o_code.Value?.ToString() ?? "500",
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
