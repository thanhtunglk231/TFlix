using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovieAsset : ICMovieAsset
    {

        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovieAsset(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> sp_get_by_id(decimal id)
        {
            try
            {
                var p_asset_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = id };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                // QUAN TRỌNG: Thứ tự phải khớp với SP nếu không BindByName
                var parameters = new IDbDataParameter[] { p_asset_id, o_code, o_message };

                // (Tùy _baseProvider) nếu có option, hãy bật BindByName = true bên trong.
                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_movie_asset_get_by_id",
                    parameters,
                    _connectionString
                );

                var code = o_code.Value?.ToString();
                var msg = o_message.Value?.ToString();

                // Phòng trường hợp provider không gán được output (vẫn có dataset)
                if (string.IsNullOrEmpty(code))
                {
                    code = dataset != null && dataset.Tables.Count > 0 && dataset.Tables[0].Rows.Count > 0 ? "200" : "404";
                    msg = code == "200" ? "Lấy dữ liệu thành công" : "Không tìm thấy dữ liệu";
                }

                return new CResponseMessage
                {
                    Data = dataset,
                    code = code,
                    message = msg ?? "",
                    Success = code == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        public async Task<CResponseMessage> sp_get_all()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_movie_assets",
                    new IDbDataParameter[] { o_code, o_message }, _connectionString);

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

        public async Task<CResponseMessage> Add(AddMovieAssetDto dto)
        {
            try
            {
                // IN
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = dto.MovieId };
                var p_asset_type = new SqlParameter("@p_asset_type", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Input, Value = dto.AssetType };
                var p_url = new SqlParameter("@p_url", SqlDbType.NVarChar, 1000) { Direction = ParameterDirection.Input, Value = dto.Url };
                var p_sort_order = new SqlParameter("@p_sort_order", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = dto.SortOrder };

                // OUT
                var o_asset_id = new SqlParameter("@o_asset_id", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
                    p_movie_id, p_asset_type, p_url, p_sort_order,
                    o_asset_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_asset_add", parameters, _connectionString);

                // ✅ Đọc OUT NUMBER đúng cách
                decimal? newAssetId = null;
                if (o_asset_id.Value != null && o_asset_id.Value != DBNull.Value)
                {
                    newAssetId = Convert.ToDecimal(o_asset_id.Value);
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, AssetId = newAssetId },
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

        public async Task<CResponseMessage> Update_episode(UpdateMovieAssetDto dto)
        {
            try
            {
                // IN
                var p_asset_id = new SqlParameter("@p_asset_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = dto.AssetId };
                var p_asset_type = new SqlParameter("@p_asset_type", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Input, Value = dto.AssetType };
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = dto.MovieId };
                var p_url = new SqlParameter("@p_url", SqlDbType.NVarChar, 1000) { Direction = ParameterDirection.Input, Value = dto.Url };
                var p_sort_order = new SqlParameter("@p_sort_order", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = dto.SortOrder };

                // OUT
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
                    p_asset_id, p_asset_type, p_url, p_sort_order,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_asset_update", parameters, _connectionString);

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

        public async Task<CResponseMessage> Delete_episode(decimal assetId)
        {
            try
            {
                // ✅ Đồng bộ kiểu NUMBER -> Decimal
                var p_asset_id = new SqlParameter("@p_asset_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = assetId };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_asset_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_asset_delete", parameters, _connectionString);

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
