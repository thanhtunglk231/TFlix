using CoreLib.Dtos.Season;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
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
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        // ===== GET ALL =====
        public async Task<CResponseMessage> get_all()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    o_code, o_message
                };

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
        public async Task<CResponseMessage> Add_season(AddSeasonDto dto)
        {
            try
            {
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = dto.SeriesId
                };

                var p_season_no = new SqlParameter("@p_season_no", SqlDbType.Int)
                {
                    Value = dto.SeasonNo
                };

                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Title ?? DBNull.Value
                };

                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Overview ?? DBNull.Value
                };

                var p_air_date = new SqlParameter("@p_air_date", SqlDbType.DateTime)
                {
                    Value = (object?)dto.AirDate ?? DBNull.Value
                };

                var o_season_id = new SqlParameter("@o_season_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
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
                        SeasonId = o_season_id.Value != DBNull.Value ? Convert.ToDecimal(o_season_id.Value) : (decimal?)null
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
                var p_season_id = new SqlParameter("@p_season_id", SqlDbType.Decimal)
                {
                    Value = dto.SeasonId
                };

                var p_season_no = new SqlParameter("@p_season_no", SqlDbType.Int)
                {
                    Value = dto.SeasonNo
                };

                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Title ?? DBNull.Value
                };

                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Overview ?? DBNull.Value
                };

                var p_air_date = new SqlParameter("@p_air_date", SqlDbType.DateTime)
                {
                    Value = (object?)dto.AirDate ?? DBNull.Value
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_season_id, p_season_no, p_title, p_overview, p_air_date,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_season_update", parameters, _connectionString);

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

        // ===== DELETE =====
        public async Task<CResponseMessage> Delete_season(decimal seasonId)
        {
            try
            {
                var p_season_id = new SqlParameter("@p_season_id", SqlDbType.Decimal)
                {
                    Value = seasonId
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_season_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_season_delete", parameters, _connectionString);

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
    }
}