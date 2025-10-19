using CoreLib.Dtos.Series;
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

namespace DataServiceLib.Implements.Admin.Series
{
    public class CSeries : ICSeries
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CSeries(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // ===== GET ALL =====
        public async Task<CResponseMessage> get_all()
        {
            try
            {
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor)
                { Direction = ParameterDirection.Output };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { o_cursor, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_series", parameters, _connectionString);

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

        // ===== ADD =====
        private static decimal? ReadNullableDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is OracleDecimal od) return od.IsNull ? null : od.Value;
            if (value is decimal d) return d;
            return Convert.ToDecimal(value); // fallback
        }

        public async Task<CResponseMessage> Add_series(AddSeriesDto dto)
        {
            try
            {
                // IN params (đúng chữ ký sp_series_add)
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, dto.Title, ParameterDirection.Input);
                var p_original_title = new OracleParameter("p_original_title", OracleDbType.Varchar2, (object?)dto.OriginalTitle ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)dto.Overview ?? DBNull.Value, ParameterDirection.Input);

                var p_first_air_date = new OracleParameter("p_first_air_date", OracleDbType.Date, (object?)dto.FirstAirDate ?? DBNull.Value, ParameterDirection.Input);
                var p_last_air_date = new OracleParameter("p_last_air_date", OracleDbType.Date, (object?)dto.LastAirDate ?? DBNull.Value, ParameterDirection.Input);

                var p_country_code = new OracleParameter("p_country_code", OracleDbType.Varchar2, (object?)dto.CountryCode ?? DBNull.Value, ParameterDirection.Input);
                var p_language_code = new OracleParameter("p_language_code", OracleDbType.Varchar2, (object?)dto.LanguageCode ?? DBNull.Value, ParameterDirection.Input);

                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, (object?)dto.Status ?? "ONGOING", ParameterDirection.Input);
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, dto.IsPremium ? "Y" : "N", ParameterDirection.Input);

                var p_imdb_id = new OracleParameter("p_imdb_id", OracleDbType.Varchar2, (object?)dto.ImdbId ?? DBNull.Value, ParameterDirection.Input);
                var p_tmdb_id = new OracleParameter("p_tmdb_id", OracleDbType.Varchar2, (object?)dto.TmdbId ?? DBNull.Value, ParameterDirection.Input);

                // OUT
                var o_series_id = new OracleParameter("o_series_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                // Giữ đúng thứ tự tham số như SP (phòng trường hợp provider không BindByName)
                var parameters = new OracleParameter[]
                {
            p_title, p_original_title, p_overview,
            p_first_air_date, p_last_air_date,
            p_country_code, p_language_code,
            p_status, p_is_premium, p_imdb_id, p_tmdb_id,
            o_series_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_series_add", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = new
                    {
                        DataSet = dataset,
                        SeriesId = ReadNullableDecimal(o_series_id.Value)   // <-- FIX cast OracleDecimal
                    },
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

        // ===== UPDATE =====
        public async Task<CResponseMessage> Update_series(UpdateSeriesDto dto)
        {
            try
            {
                // IN parameters: theo đúng chữ ký sp_series_update
                var p_series_id = new OracleParameter("p_series_id", OracleDbType.Decimal, dto.SeriesId, ParameterDirection.Input);

                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, dto.Title, ParameterDirection.Input);
                var p_original_title = new OracleParameter("p_original_title", OracleDbType.Varchar2, (object?)dto.OriginalTitle ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)dto.Overview ?? DBNull.Value, ParameterDirection.Input);

                var p_first_air_date = new OracleParameter("p_first_air_date", OracleDbType.Date, (object?)dto.FirstAirDate ?? DBNull.Value, ParameterDirection.Input);
                var p_last_air_date = new OracleParameter("p_last_air_date", OracleDbType.Date, (object?)dto.LastAirDate ?? DBNull.Value, ParameterDirection.Input);

                var p_country_code = new OracleParameter("p_country_code", OracleDbType.Varchar2, (object?)dto.CountryCode ?? DBNull.Value, ParameterDirection.Input);
                var p_language_code = new OracleParameter("p_language_code", OracleDbType.Varchar2, (object?)dto.LanguageCode ?? DBNull.Value, ParameterDirection.Input);

                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, (object?)dto.Status ?? "ONGOING", ParameterDirection.Input);
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, dto.IsPremium ? "Y" : "N", ParameterDirection.Input);

                var p_imdb_id = new OracleParameter("p_imdb_id", OracleDbType.Varchar2, (object?)dto.ImdbId ?? DBNull.Value, ParameterDirection.Input);
                var p_tmdb_id = new OracleParameter("p_tmdb_id", OracleDbType.Varchar2, (object?)dto.TmdbId ?? DBNull.Value, ParameterDirection.Input);

                // OUT
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_series_id,
                    p_title, p_original_title, p_overview,
                    p_first_air_date, p_last_air_date,
                    p_country_code, p_language_code,
                    p_status, p_is_premium, p_imdb_id, p_tmdb_id,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_series_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // SP update không mở cursor
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

        // ===== DELETE =====
        public async Task<CResponseMessage> Delete_series(decimal seriesId)
        {
            try
            {
                var p_series_id = new OracleParameter("p_series_id", OracleDbType.Decimal, seriesId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_series_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_series_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // SP delete không mở cursor
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
