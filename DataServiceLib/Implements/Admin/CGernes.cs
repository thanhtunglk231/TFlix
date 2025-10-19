using CoreLib.Dtos.Genres;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
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
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // ========== GET ALL ==========
        public async Task<CResponseMessage> GetAll()
        {
            try
            {
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { o_cursor, o_code, o_message };

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
                var p_genre_name = new OracleParameter("p_genre_name", OracleDbType.Varchar2, (object?)dto.genreName ?? DBNull.Value, ParameterDirection.Input);
                var p_slug = new OracleParameter("p_slug", OracleDbType.Varchar2, (object?)dto.slug ?? DBNull.Value, ParameterDirection.Input);

                var o_genre_id = new OracleParameter("o_genre_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_genre_name, p_slug, o_genre_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_genre_add", parameters, _connectionString);

                // Đọc OUTPUT NUMBER (Oracle NUMBER -> OracleDecimal)
                decimal? genreId = null;
                if (o_genre_id.Value != DBNull.Value)
                {
                    var od = (OracleDecimal)o_genre_id.Value;
                    if (!od.IsNull) genreId = od.Value; // decimal
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
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, dto.GenreId, ParameterDirection.Input);
                var p_genre_name = new OracleParameter("p_genre_name", OracleDbType.Varchar2, (object?)dto.genreName ?? DBNull.Value, ParameterDirection.Input);
                var p_slug = new OracleParameter("p_slug", OracleDbType.Varchar2, (object?)dto.slug ?? DBNull.Value, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_genre_id, p_genre_name, p_slug, o_code, o_message };

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
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, genreId, ParameterDirection.Input);
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_genre_id, o_code, o_message };

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
