// DataServiceLib/Implements/CEpisode.cs
using CoreLib.Dtos.Episode;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;   // ✅ THÊM
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CEpisode : ICEpisode
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CEpisode(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public async Task<CResponseMessage> sp_get_all_episode()
        {
            try
            {
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { o_cursor, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_episode", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }


        public async Task<CResponseMessage> sp_get_by_id(decimal id)
        {
            try
            {
                var p_asset_id = new OracleParameter("p_asset_id", OracleDbType.Decimal, 10) { Direction = ParameterDirection.Input, Value=id };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { o_cursor, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_asset_get_by_id", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        public async Task<CResponseMessage> Add_episode(AddEpisodeDto addEpisodeDto)
        {
            try
            {
                // IN
                var p_series_id = new OracleParameter("p_series_id", OracleDbType.Decimal, addEpisodeDto.SeriesId, ParameterDirection.Input);
                var p_season_id = new OracleParameter("p_season_id", OracleDbType.Decimal, addEpisodeDto.SeasonId, ParameterDirection.Input);
                var p_episode_no = new OracleParameter("p_episode_no", OracleDbType.Int32, addEpisodeDto.EpisodeNo, ParameterDirection.Input);
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, addEpisodeDto.Title, ParameterDirection.Input);
                var p_air_date = new OracleParameter("p_air_date", OracleDbType.Date, (object?)addEpisodeDto.AirDate ?? DBNull.Value, ParameterDirection.Input);
                var p_duration_min = new OracleParameter("p_duration_min", OracleDbType.Int32, (object?)addEpisodeDto.DurationMin ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)addEpisodeDto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(addEpisodeDto.Status) ? "PUBLISHED" : addEpisodeDto.Status, ParameterDirection.Input);
                // ✅ CHAR(1) + truyền đúng 'Y'/'N'
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, 1, addEpisodeDto.IsPremium ? "Y" : "N", ParameterDirection.Input);

                // OUT
                var o_episode_id = new OracleParameter("o_episode_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_series_id, p_season_id, p_episode_no, p_title,
                    p_air_date, p_duration_min, p_overview, p_status, p_is_premium,
                    o_episode_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_add", parameters, _connectionString);

                // ✅ ĐỌC OracleDecimal ĐÚNG CÁCH
                decimal? newId = null;
                if (o_episode_id.Value != null && o_episode_id.Value != DBNull.Value)
                {
                    var od = (OracleDecimal)o_episode_id.Value;
                    if (!od.IsNull) newId = od.Value;
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, EpisodeId = newId },
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        public async Task<CResponseMessage> Update_episode(UpdateEpisodeDto dto)
        {
            try
            {
                var p_episode_id = new OracleParameter("p_episode_id", OracleDbType.Decimal, dto.EpisodeId, ParameterDirection.Input);
                var p_series_id = new OracleParameter("p_series_id", OracleDbType.Decimal,
                        (object?)dto.SeriesId ?? DBNull.Value, ParameterDirection.Input);
                var p_season_id = new OracleParameter("p_season_id", OracleDbType.Decimal,
                                        (object?)dto.SeasonId ?? DBNull.Value, ParameterDirection.Input);
                var p_title = new OracleParameter("p_title", OracleDbType.Varchar2, dto.Title, ParameterDirection.Input);
                var p_episode_no = new OracleParameter("p_episode_no", OracleDbType.Int32, dto.EpisodeNo, ParameterDirection.Input);
                var p_air_date = new OracleParameter("p_air_date", OracleDbType.Date, (object?)dto.AirDate ?? DBNull.Value, ParameterDirection.Input);
                var p_duration_min = new OracleParameter("p_duration_min", OracleDbType.Int32, (object?)dto.DurationMin ?? DBNull.Value, ParameterDirection.Input);
                var p_overview = new OracleParameter("p_overview", OracleDbType.Clob, (object?)dto.Overview ?? DBNull.Value, ParameterDirection.Input);
                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(dto.Status) ? "PUBLISHED" : dto.Status, ParameterDirection.Input);
                var p_is_premium = new OracleParameter("p_is_premium", OracleDbType.Char, 1, dto.IsPremium ? "Y" : "N", ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_episode_id,p_series_id,p_season_id, p_title, p_episode_no, p_air_date,
                    p_duration_min, p_overview, p_status, p_is_premium,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        public async Task<CResponseMessage> Delete_episode(decimal episodeId)
        {
            try
            {
                var p_episode_id = new OracleParameter("p_episode_id", OracleDbType.Decimal, episodeId, ParameterDirection.Input);
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_episode_id, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }
    }
}
