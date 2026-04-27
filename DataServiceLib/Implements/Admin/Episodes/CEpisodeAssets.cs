using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataServiceLib.Implements.Admin.Episodes
{
    public class CEpisodeAssets : ICEpisodeAssets
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CEpisodeAssets(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> sp_get_by_id(decimal id)
        {
            try
            {
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Input,
                    Value = id
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_episode_id,
                    o_code,
                    o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_episode_asset_get_by_id",
                    parameters,
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

        public async Task<CResponseMessage> sp_get_all()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    o_code,
                    o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_get_all_episode_assets",
                    parameters,
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

        public async Task<CResponseMessage> Add(AddEpisodeAsset dto)
        {
            try
            {
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.EpisodeId
                };

                var p_asset_type = new SqlParameter("@p_asset_type", SqlDbType.NVarChar, 30)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.AssetType
                };

                var p_url = new SqlParameter("@p_url", SqlDbType.NVarChar, 1000)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.Url
                };

                var p_sort_order = new SqlParameter("@p_sort_order", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.SortOrder
                };

                var o_asset_id = new SqlParameter("@o_asset_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_episode_id,
                    p_asset_type,
                    p_url,
                    p_sort_order,
                    o_asset_id,
                    o_code,
                    o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_episode_asset_add",
                    parameters,
                    _connectionString
                );

                decimal? newAssetId = null;
                if (o_asset_id.Value != null && o_asset_id.Value != DBNull.Value)
                {
                    newAssetId = Convert.ToDecimal(o_asset_id.Value);
                }

                return new CResponseMessage
                {
                    Data = new
                    {
                        DataSet = dataset,
                        AssetId = newAssetId
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

        public async Task<CResponseMessage> Update_episode(UpdateEpisodeAsset dto)
        {
            try
            {
                var p_asset_id = new SqlParameter("@p_asset_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.AssetId
                };

                var p_asset_type = new SqlParameter("@p_asset_type", SqlDbType.NVarChar, 30)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.AssetType
                };

                var p_url = new SqlParameter("@p_url", SqlDbType.NVarChar, 1000)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.Url
                };

                var p_sort_order = new SqlParameter("@p_sort_order", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = dto.SortOrder
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_asset_id,
                    p_asset_type,
                    p_url,
                    p_sort_order,
                    o_code,
                    o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_episode_asset_update",
                    parameters,
                    _connectionString
                );

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

        public async Task<CResponseMessage> Delete_episode(decimal assetId)
        {
            try
            {
                var p_asset_id = new SqlParameter("@p_asset_id", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Input,
                    Value = assetId
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_asset_id,
                    o_code,
                    o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_episode_asset_delete",
                    parameters,
                    _connectionString
                );

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