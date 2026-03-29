using CoreLib.Dtos;
using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin
{
    public class CVideoSoure : ICVideoSoure
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CVideoSoure(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        // ===== GET ALL =====
        public CResponseMessage get_all()
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

                var parameters = new IDbDataParameter[] { o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_video_sources", parameters, _connectionString);

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

        // ===== GET BY ID =====
        public CResponseMessage get_bu_id(int id)
        {
            try
            {
                var p_id = new SqlParameter("@p_id", SqlDbType.Int)
                {
                    Value = id
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { p_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_videosources_by_id", parameters, _connectionString);

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
        public async Task<CResponseMessage> Add_video_source(AddVideoSourceDto dto)
        {
            try
            {
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal)
                {
                    Value = (object?)dto.MovieId ?? DBNull.Value
                };

                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.Decimal)
                {
                    Value = (object?)dto.EpisodeId ?? DBNull.Value
                };

                var p_provider = new SqlParameter("@p_provider", SqlDbType.NVarChar)
                {
                    Value = dto.Provider
                };

                var p_server = new SqlParameter("@p_server_name", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.ServerName ?? DBNull.Value
                };

                var p_stream_url = new SqlParameter("@p_stream_url", SqlDbType.NVarChar)
                {
                    Value = dto.StreamUrl
                };

                var p_quality = new SqlParameter("@p_quality", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Quality ?? DBNull.Value
                };

                var p_format = new SqlParameter("@p_format", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Format ?? DBNull.Value
                };

                var p_drm_type = new SqlParameter("@p_drm_type", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.DrmType ?? DBNull.Value
                };

                var p_drm_url = new SqlParameter("@p_drm_license_url", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.DrmLicenseUrl ?? DBNull.Value
                };

                var p_is_primary = new SqlParameter("@p_is_primary", SqlDbType.Char, 1)
                {
                    Value = dto.IsPrimary ? "Y" : "N"
                };

                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Status ?? "ACTIVE"
                };

                var o_source_id = new SqlParameter("@o_source_id", SqlDbType.Decimal)
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
                    p_movie_id, p_episode_id, p_provider, p_server, p_stream_url,
                    p_quality, p_format, p_drm_type, p_drm_url,
                    p_is_primary, p_status,
                    o_source_id, o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_add", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = new
                    {
                        DataSet = ds,
                        SourceId = o_source_id.Value != DBNull.Value ? Convert.ToDecimal(o_source_id.Value) : (decimal?)null
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

        // ===== ADD PART =====
        public async Task<CResponseMessage> Add_video_source_part(AddVideoSourcePartDto dto)
        {
            try
            {
                var p_source_id = new SqlParameter("@p_source_id", SqlDbType.Decimal)
                {
                    Value = dto.SourceId
                };

                var p_part_index = new SqlParameter("@p_part_index", SqlDbType.Int)
                {
                    Value = dto.PartIndex
                };

                var p_url = new SqlParameter("@p_url", SqlDbType.NVarChar)
                {
                    Value = dto.Url
                };

                var p_byte_size = new SqlParameter("@p_byte_size", SqlDbType.BigInt)
                {
                    Value = (object?)dto.ByteSize ?? DBNull.Value
                };

                var p_duration_sec = new SqlParameter("@p_duration_sec", SqlDbType.Int)
                {
                    Value = (object?)dto.DurationSec ?? DBNull.Value
                };

                var p_checksum = new SqlParameter("@p_checksum", SqlDbType.NVarChar)
                {
                    Value = (object?)dto.Checksum ?? DBNull.Value
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
                    p_source_id, p_part_index, p_url,
                    p_byte_size, p_duration_sec, p_checksum,
                    o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_part_add", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = new
                    {
                        DataSet = ds,
                        SourceId = dto.SourceId,
                        PartIndex = dto.PartIndex
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
        public async Task<CResponseMessage> Update_video_source(UpdateVideoSourceDto dto)
        {
            try
            {
                var p_source_id = new SqlParameter("@p_source_id", SqlDbType.Decimal) { Value = dto.SourceId };
                var p_provider = new SqlParameter("@p_provider", SqlDbType.NVarChar) { Value = dto.Provider };
                var p_server = new SqlParameter("@p_server_name", SqlDbType.NVarChar) { Value = (object?)dto.ServerName ?? DBNull.Value };
                var p_stream_url = new SqlParameter("@p_stream_url", SqlDbType.NVarChar) { Value = dto.StreamUrl };
                var p_quality = new SqlParameter("@p_quality", SqlDbType.NVarChar) { Value = (object?)dto.Quality ?? DBNull.Value };
                var p_format = new SqlParameter("@p_format", SqlDbType.NVarChar) { Value = (object?)dto.Format ?? DBNull.Value };
                var p_drm_type = new SqlParameter("@p_drm_type", SqlDbType.NVarChar) { Value = (object?)dto.DrmType ?? DBNull.Value };
                var p_drm_url = new SqlParameter("@p_drm_license_url", SqlDbType.NVarChar) { Value = (object?)dto.DrmLicenseUrl ?? DBNull.Value };
                var p_is_primary = new SqlParameter("@p_is_primary", SqlDbType.Char, 1) { Value = dto.IsPrimary ? "Y" : "N" };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar) { Value = (object?)dto.Status ?? "ACTIVE" };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
                    p_source_id, p_provider, p_server, p_stream_url,
                    p_quality, p_format, p_drm_type, p_drm_url,
                    p_is_primary, p_status,
                    o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
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
        public async Task<CResponseMessage> Delete_video_source(decimal sourceId)
        {
            try
            {
                var p_source_id = new SqlParameter("@p_source_id", SqlDbType.Decimal)
                {
                    Value = sourceId
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
                    p_source_id, o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
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