// DataServiceLib/Implements/CEpisode.cs
using CoreLib.Dtos.Episode;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.Episodes
{
    public class CEpisode : ICEpisode
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CEpisode(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> sp_get_all_episode()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_episode", parameters, _connectionString);

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
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }


        public async Task<CResponseMessage> sp_get_by_id(decimal id)
        {
            try
            {
                var p_asset_id = new SqlParameter("@p_asset_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = id };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_asset_id, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_asset_get_by_id", parameters, _connectionString);

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
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        public async Task<CResponseMessage> Add_episode(AddEpisodeDto addEpisodeDto)
        {
            try
            {
                // IN
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = addEpisodeDto.SeriesId };
                var p_season_id = new SqlParameter("@p_season_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = addEpisodeDto.SeasonId };
                var p_episode_no = new SqlParameter("@p_episode_no", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = addEpisodeDto.EpisodeNo };
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)addEpisodeDto.Title ?? DBNull.Value };
                var p_air_date = new SqlParameter("@p_air_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)addEpisodeDto.AirDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)addEpisodeDto.DurationMin ?? DBNull.Value };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)addEpisodeDto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = string.IsNullOrWhiteSpace(addEpisodeDto.Status) ? "PUBLISHED" : addEpisodeDto.Status };
                // ✅ CHAR(1) + truyền đúng 'Y'/'N'
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = addEpisodeDto.IsPremium ? "Y" : "N" };

                // OUT

                var o_episode_id = new SqlParameter("@o_episode_id", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
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
                    newId = Convert.ToDecimal(o_episode_id.Value);
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, EpisodeId = newId },
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
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
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = dto.EpisodeId };
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = (object?)dto.SeriesId ?? DBNull.Value };
                var p_season_id = new SqlParameter("@p_season_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = (object?)dto.SeasonId ?? DBNull.Value };
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)dto.Title ?? DBNull.Value };
                var p_episode_no = new SqlParameter("@p_episode_no", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = dto.EpisodeNo };
                var p_air_date = new SqlParameter("@p_air_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)dto.AirDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)dto.DurationMin ?? DBNull.Value };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)dto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = string.IsNullOrWhiteSpace(dto.Status) ? "PUBLISHED" : dto.Status };
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = dto.IsPremium ? "Y" : "N" };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
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
                    Success = o_code.Value?.ToString() == "200"
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
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = episodeId };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_episode_id, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_delete", parameters, _connectionString);

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
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }
    }
}
