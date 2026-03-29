using CoreLib.Dtos.Genres;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin
{
    public class CGenres : ICGenres
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CGenres(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        // ========== GET ALL ==========
        public async Task<CResponseMessage> GetAll()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_genre_get_all", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
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

        // ========== ADD ==========
        public async Task<CResponseMessage> Add(AddGenreDto dto)
        {
            try
            {
                var p_genre_name = new SqlParameter("@p_genre_name", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Input, Value = (object?)dto.genreName ?? DBNull.Value };
                var p_slug = new SqlParameter("@p_slug", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Input, Value = (object?)dto.slug ?? DBNull.Value };

                var o_genre_id = new SqlParameter("@o_genre_id", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_genre_name, p_slug, o_genre_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_genre_add", parameters, _connectionString);

                // Đọc OUTPUT NUMBER (SQL Server decimal)
                decimal? genreId = null;
                if (o_genre_id.Value != DBNull.Value && o_genre_id.Value != null)
                {
                    genreId = Convert.ToDecimal(o_genre_id.Value);
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, GenreId = genreId },
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

        // ========== UPDATE ==========
        public async Task<CResponseMessage> Update(UpdateGenreDto dto)
        {
            try
            {
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = dto.GenreId };
                var p_genre_name = new SqlParameter("@p_genre_name", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Input, Value = (object?)dto.genreName ?? DBNull.Value };
                var p_slug = new SqlParameter("@p_slug", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Input, Value = (object?)dto.slug ?? DBNull.Value };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_genre_id, p_genre_name, p_slug, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_genre_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // SP không trả cursor
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

        // ========== DELETE ==========
        public async Task<CResponseMessage> Delete(decimal genreId)
        {
            try
            {
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = genreId };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_genre_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_genre_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // không có cursor
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
