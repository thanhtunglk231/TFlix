using CoreLib.Dtos.VideSoure;
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

namespace DataServiceLib.Implements.Admin
{
    public class CVideoSoure : ICVideoSoure
    {

        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CVideoSoure(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public  CResponseMessage get_all()
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


        public CResponseMessage get_bu_id(int id)
        {
            try
            {
                var o_cursor = new OracleParameter("p_id", OracleDbType.Int32)
                { Direction = ParameterDirection.Output };
                var p_result = new OracleParameter("p_result", OracleDbType.RefCursor)
                { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { o_cursor, p_result, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_videosources_by_id ", parameters, _connectionString);

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

        // Helper: đọc OUT NUMBER an toàn
        private static decimal? ReadNullableDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is OracleDecimal od) return od.IsNull ? null : od.Value;
            if (value is decimal d) return d;
            return Convert.ToDecimal(value);
        }

        // ================== ADD ==================
        public async Task<CResponseMessage> Add_video_source(AddVideoSourceDto dto)
        {
            try
            {
                // Kiểm tra logic: phải chọn đúng 1 đích (movie hoặc episode)
                var movieProvided = dto.MovieId.HasValue;
                var episodeProvided = dto.EpisodeId.HasValue;
                if (movieProvided == episodeProvided) // cả 2 null hoặc cả 2 có giá trị
                {
                    return new CResponseMessage
                    {
                        Success = false,
                        code = "400",
                        message = "Phải chọn duy nhất một đích: movieId hoặc episodeId."
                    };
                }

                // Tham số theo đúng thứ tự của sp_video_source_add:
                // (p_movie_id, p_episode_id, p_provider, p_server_name, p_stream_url,
                //  p_quality, p_format, p_drm_type, p_drm_license_url, p_is_primary, p_status,
                //  o_source_id, o_code, o_message)

                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal)
                { Direction = ParameterDirection.Input, Value = (object?)dto.MovieId ?? DBNull.Value };

                var p_episode_id = new OracleParameter("p_episode_id", OracleDbType.Decimal)
                { Direction = ParameterDirection.Input, Value = (object?)dto.EpisodeId ?? DBNull.Value };

                var p_provider = new OracleParameter("p_provider", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = dto.Provider };

                var p_server = new OracleParameter("p_server_name", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.ServerName ?? DBNull.Value };

                var p_stream_url = new OracleParameter("p_stream_url", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = dto.StreamUrl };

                var p_quality = new OracleParameter("p_quality", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.Quality ?? DBNull.Value };

                var p_format = new OracleParameter("p_format", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.Format ?? DBNull.Value };

                var p_drm_type = new OracleParameter("p_drm_type", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.DrmType ?? DBNull.Value };

                var p_drm_url = new OracleParameter("p_drm_license_url", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.DrmLicenseUrl ?? DBNull.Value };

                var p_is_primary = new OracleParameter("p_is_primary", OracleDbType.Char, 1)
                { Direction = ParameterDirection.Input, Value = dto.IsPrimary ? "Y" : "N" };

                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2)
                { Direction = ParameterDirection.Input, Value = (object?)dto.Status ?? "ACTIVE" };

                var o_source_id = new OracleParameter("o_source_id", OracleDbType.Decimal)
                { Direction = ParameterDirection.Output };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                { Direction = ParameterDirection.Output };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                { Direction = ParameterDirection.Output };


                var parameters = new OracleParameter[]
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
                        SourceId = ReadNullableDecimal(o_source_id.Value)
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

        // ================== UPDATE ==================
        public async Task<CResponseMessage> Update_video_source(UpdateVideoSourceDto dto)
        {
            try
            {
                // sp_video_source_update (
                //   p_source_id, p_provider, p_server_name, p_stream_url,
                //   p_quality, p_format, p_drm_type, p_drm_license_url,
                //   p_is_primary, p_status, o_code, o_message)

                var p_source_id = new OracleParameter("p_source_id", OracleDbType.Decimal, dto.SourceId, ParameterDirection.Input);
                var p_provider = new OracleParameter("p_provider", OracleDbType.Varchar2, dto.Provider, ParameterDirection.Input);
                var p_server = new OracleParameter("p_server_name", OracleDbType.Varchar2, (object?)dto.ServerName ?? DBNull.Value, ParameterDirection.Input);
                var p_stream_url = new OracleParameter("p_stream_url", OracleDbType.Varchar2, dto.StreamUrl, ParameterDirection.Input);
                var p_quality = new OracleParameter("p_quality", OracleDbType.Varchar2, (object?)dto.Quality ?? DBNull.Value, ParameterDirection.Input);
                var p_format = new OracleParameter("p_format", OracleDbType.Varchar2, (object?)dto.Format ?? DBNull.Value, ParameterDirection.Input);
                var p_drm_type = new OracleParameter("p_drm_type", OracleDbType.Varchar2, (object?)dto.DrmType ?? DBNull.Value, ParameterDirection.Input);
                var p_drm_url = new OracleParameter("p_drm_license_url", OracleDbType.Varchar2, (object?)dto.DrmLicenseUrl ?? DBNull.Value, ParameterDirection.Input);
                var p_is_primary = new OracleParameter("p_is_primary", OracleDbType.Char, dto.IsPrimary ? "Y" : "N", ParameterDirection.Input);
                var p_status = new OracleParameter("p_status", OracleDbType.Varchar2, (object?)dto.Status ?? "ACTIVE", ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_source_id, p_provider, p_server, p_stream_url,
                    p_quality, p_format, p_drm_type, p_drm_url,
                    p_is_primary, p_status,
                    o_code, o_message
                };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = ds, // SP update không mở cursor
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

        // ================== DELETE ==================
        public async Task<CResponseMessage> Delete_video_source(decimal sourceId)
        {
            try
            {
                // sp_video_source_delete(p_source_id, o_code, o_message)
                var p_source_id = new OracleParameter("p_source_id", OracleDbType.Decimal, sourceId, ParameterDirection.Input);
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_source_id, o_code, o_message };

                var ds = _baseProvider.GetDatasetFromSP("sp_video_source_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = ds, // SP delete không mở cursor
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
