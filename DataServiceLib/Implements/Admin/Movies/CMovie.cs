using CoreLib.Dtos.AuthDtos;
using CoreLib.Dtos.Movies;
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

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovie : ICMovie
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovie(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }


        public async Task<CResponseMessage> get_all()
        {
            try
            {


                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { o_code, o_message };

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
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Title ?? DBNull.Value };
                var p_release_date = new SqlParameter("@p_release_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.ReleaseDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.DurationMin ?? DBNull.Value };
                // Trim codes to typical 2-char ISO codes to avoid DB truncation if DB column is short
                string? lang = addMovieDto.LanguageCode;
                if (!string.IsNullOrWhiteSpace(lang) && lang.Length > 2) lang = lang.Substring(0, 2);
                string? country = addMovieDto.CountryCode;
                if (!string.IsNullOrWhiteSpace(country) && country.Length > 2) country = country.Substring(0, 2);
                var p_language_code = new SqlParameter("@p_language_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)lang ?? DBNull.Value };

                var p_original_title = new SqlParameter("@p_original_title", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.OriginalTitle ?? DBNull.Value };
                var p_country_code = new SqlParameter("@p_country_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)country ?? DBNull.Value };
                // ✅ CHAR(1) cho Y/N
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = addMovieDto.IsPremium ? "Y" : "N" };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Status ?? "PUBLISHED" };

                // OUT parameters
                var o_movie_id = new SqlParameter("@o_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
            p_title,p_original_title, p_release_date, p_duration_min,
            p_language_code, p_country_code, p_is_premium,
            p_overview, p_status,
            o_movie_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_add", parameters, _connectionString);

                // ✅ ĐỌC OUT NUMBER ĐÚNG CÁCH
                decimal? newId = null;
                if (o_movie_id.Value != null && o_movie_id.Value != DBNull.Value)
                {
                    newId = Convert.ToDecimal(o_movie_id.Value);
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
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = updateMovieDto.MovieId };
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Title ?? DBNull.Value };
                var p_original_title = new SqlParameter("@p_original_title", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.OriginalTitle ?? DBNull.Value };

                var p_release_date = new SqlParameter("@p_release_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.ReleaseDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.DurationMin ?? DBNull.Value };
                string? ulang = updateMovieDto.LanguageCode;
                if (!string.IsNullOrWhiteSpace(ulang) && ulang.Length > 2) ulang = ulang.Substring(0, 2);
                string? ucountry = updateMovieDto.CountryCode;
                if (!string.IsNullOrWhiteSpace(ucountry) && ucountry.Length > 2) ucountry = ucountry.Substring(0, 2);

                var p_language_code = new SqlParameter("@p_language_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)ulang ?? DBNull.Value };
                var p_country_code = new SqlParameter("@p_country_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)ucountry ?? DBNull.Value };
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = updateMovieDto.IsPremium ? "Y" : "N" };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Status ?? "PUBLISHED" };

                // OUT parameters
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
            p_movie_id, p_title,p_original_title, p_release_date, p_duration_min,
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
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = movieId };

                // OUT parameters
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, o_code, o_message };

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
