using CoreLib.Dtos.Season;

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
    public class CSeason : ICSeason
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CSeason(ICBaseProvider baseProvider, IConfiguration configuration)
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

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_season", parameters, _connectionString);

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

        public async Task<CResponseMessage> Add_season(AddSeasonDto dto)
        {
            try
            {
                // IN params theo chữ ký sp_season_add:
                // (p_series_id, p_season_no, p_title, p_overview, p_air_date, o_season_id, o_code, o_message)
                var p_series_id = new OracleParameter("p_series_id", OracleDbType.Decimal, dto.SeriesId, ParameterDirection.Input);
                var p_season_no = new OracleParameter("p_season_no", OracleDbType.Int32, dto.SeasonNo, ParameterDirection.Input);
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, (object?)dto.Title ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)dto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_air_date = new OracleParameter("p_air_date", OracleDbType.Date, (object?)dto.AirDate ?? DBNull.Value, ParameterDirection.Input);

                // OUT
                var o_season_id = new OracleParameter("o_season_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                // LƯU Ý: Giữ đúng thứ tự tham số như SP nếu chưa bật BindByName
                var parameters = new OracleParameter[]
                {
            p_series_id, p_season_no, p_title, p_overview, p_air_date,
            o_season_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_season_add", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = new
                    {
                        DataSet = dataset,
                        SeasonId = ReadNullableDecimal(o_season_id.Value) // <-- FIX cast OracleDecimal
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
        public async Task<CResponseMessage> Update_season(UpdateSeasonDto dto)
        {
            try
            {
                // IN params theo sp_season_update(p_season_id, p_season_no, p_title, p_overview, p_air_date, ...)
                var p_season_id = new OracleParameter("p_season_id", OracleDbType.Decimal, dto.SeasonId, ParameterDirection.Input);
                var p_season_no = new OracleParameter("p_season_no", OracleDbType.Int32, dto.SeasonNo, ParameterDirection.Input);
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, (object?)dto.Title ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)dto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_air_date = new OracleParameter("p_air_date", OracleDbType.Date, (object?)dto.AirDate ?? DBNull.Value, ParameterDirection.Input);

                // OUT
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_season_id, p_season_no, p_title, p_overview, p_air_date,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_season_update", parameters, _connectionString);

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
        public async Task<CResponseMessage> Delete_season(decimal seasonId)
        {
            try
            {
                var p_season_id = new OracleParameter("p_season_id", OracleDbType.Decimal, seasonId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_season_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_season_delete", parameters, _connectionString);

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
