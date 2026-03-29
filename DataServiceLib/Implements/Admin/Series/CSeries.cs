using CoreLib.Dtos.Series;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
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
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        // ===== GET ALL =====
        public async Task<CResponseMessage> get_all()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                { Direction = ParameterDirection.Output };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_get_all_series",
                    new IDbDataParameter[] { o_code, o_message },
                    _connectionString
                );

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
        public async Task<CResponseMessage> Add_series(AddSeriesDto dto)
        {
            try
            {
                var parameters = new IDbDataParameter[]
                {
            // SERIES
            new SqlParameter("@p_title", SqlDbType.NVarChar, 255)
            {
                Value = dto.Title
            },
            new SqlParameter("@p_original_title", SqlDbType.NVarChar, 255)
            {
                Value = (object?)dto.OriginalTitle ?? DBNull.Value
            },
            new SqlParameter("@p_overview", SqlDbType.NVarChar)
            {
                Value = (object?)dto.Overview ?? DBNull.Value
            },
            new SqlParameter("@p_first_air_date", SqlDbType.Date)
            {
                Value = (object?)dto.FirstAirDate ?? DBNull.Value
            },
            new SqlParameter("@p_country_code", SqlDbType.NVarChar, 10)
            {
                Value = (object?)dto.CountryCode ?? DBNull.Value
            },
            new SqlParameter("@p_language_code", SqlDbType.NVarChar, 10)
            {
                Value = (object?)dto.LanguageCode ?? DBNull.Value
            },
            new SqlParameter("@p_status", SqlDbType.NVarChar, 50)
            {
                Value = (object?)dto.Status ?? "ONGOING"
            },
            new SqlParameter("@p_is_premium", SqlDbType.Char, 1)
            {
                Value = dto.IsPremium ? "Y" : "N"
            },

            // SEASON
            new SqlParameter("@p_season_no", SqlDbType.Int)
            {
                Value = dto.SeasonNo <= 0 ? 1 : dto.SeasonNo
            },
            new SqlParameter("@p_season_title", SqlDbType.NVarChar, 255)
            {
                Value = (object?)dto.SeasonTitle ?? DBNull.Value
            },
            new SqlParameter("@p_season_overview", SqlDbType.NVarChar)
            {
                Value = (object?)dto.SeasonOverview ?? DBNull.Value
            },
            new SqlParameter("@p_season_air_date", SqlDbType.Date)
            {
                Value = (object?)dto.SeasonAirDate ?? DBNull.Value
            },

            // EPISODES JSON
            new SqlParameter("@p_episodes_json", SqlDbType.NVarChar)
            {
                Value = string.IsNullOrWhiteSpace(dto.EpisodesJson) ? "[]" : dto.EpisodesJson
            },

            // OUTPUT
            new SqlParameter("@o_series_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            },
            new SqlParameter("@o_season_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            },
            new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
            {
                Direction = ParameterDirection.Output
            },
            new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
            {
                Direction = ParameterDirection.Output
            }
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_series_add_with_season_eps",
                    parameters,
                    _connectionString
                );

                var seriesIdObj = parameters[13].Value;
                var seasonIdObj = parameters[14].Value;
                var codeObj = parameters[15].Value;
                var messageObj = parameters[16].Value;

                var code = codeObj?.ToString() ?? "500";
                var message = messageObj?.ToString() ?? "Không có phản hồi";

                int? seriesId = null;
                if (seriesIdObj != null && seriesIdObj != DBNull.Value)
                    seriesId = Convert.ToInt32(seriesIdObj);

                int? seasonId = null;
                if (seasonIdObj != null && seasonIdObj != DBNull.Value)
                    seasonId = Convert.ToInt32(seasonIdObj);

                return new CResponseMessage
                {
                    Success = code == "200",
                    code = code,
                    message = message,
                    Data = new
                    {
                        SeriesId = seriesId,
                        SeasonId = seasonId,
                        Episodes = dataset
                    }
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
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_series_id", SqlDbType.Decimal) { Value = dto.SeriesId },

                    new SqlParameter("@p_title", SqlDbType.NVarChar) { Value = dto.Title },
                    new SqlParameter("@p_original_title", SqlDbType.NVarChar) { Value = (object?)dto.OriginalTitle ?? DBNull.Value },
                    new SqlParameter("@p_overview", SqlDbType.NVarChar) { Value = (object?)dto.Overview ?? DBNull.Value },

                    new SqlParameter("@p_first_air_date", SqlDbType.DateTime) { Value = (object?)dto.FirstAirDate ?? DBNull.Value },
                    new SqlParameter("@p_last_air_date", SqlDbType.DateTime) { Value = (object?)dto.LastAirDate ?? DBNull.Value },

                    new SqlParameter("@p_country_code", SqlDbType.NVarChar) { Value = (object?)dto.CountryCode ?? DBNull.Value },
                    new SqlParameter("@p_language_code", SqlDbType.NVarChar) { Value = (object?)dto.LanguageCode ?? DBNull.Value },

                    new SqlParameter("@p_status", SqlDbType.NVarChar) { Value = (object?)dto.Status ?? "ONGOING" },
                    new SqlParameter("@p_is_premium", SqlDbType.Char) { Value = dto.IsPremium ? "Y" : "N" },

                    new SqlParameter("@p_imdb_id", SqlDbType.NVarChar) { Value = (object?)dto.ImdbId ?? DBNull.Value },
                    new SqlParameter("@p_tmdb_id", SqlDbType.NVarChar) { Value = (object?)dto.TmdbId ?? DBNull.Value },

                    new SqlParameter("@o_code", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output },
                    new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output }
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_series_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = parameters[^2].Value?.ToString() ?? "500",
                    message = parameters[^1].Value?.ToString() ?? "Không có phản hồi",
                    Success = parameters[^2].Value?.ToString() == "200"
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
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_series_id", SqlDbType.Decimal) { Value = seriesId },
                    new SqlParameter("@o_code", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output },
                    new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output }
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_series_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = parameters[1].Value?.ToString() ?? "500",
                    message = parameters[2].Value?.ToString() ?? "Không có phản hồi",
                    Success = parameters[1].Value?.ToString() == "200"
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