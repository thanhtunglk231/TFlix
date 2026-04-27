using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types; // ✅ THÊM
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
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public async Task<CResponseMessage> sp_get_by_id(decimal id)
        {
            try
            {
                var p_asset_id = new OracleParameter("p_episode_id", OracleDbType.Decimal)
                {
                    Direction = ParameterDirection.Input,
                    Value = id
                };

                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor)
                {
                    Direction = ParameterDirection.Output
                };

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                // QUAN TRỌNG: Thứ tự phải khớp với SP nếu không BindByName
                var parameters = new OracleParameter[] { p_asset_id, o_cursor, o_code, o_message };

                // (Tùy _baseProvider) nếu có option, hãy bật BindByName = true bên trong.
                var dataset = _baseProvider.GetDatasetFromSP(
                    "sp_episode_asset_get_by_id",
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
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_episode_assets",
                    new[] { o_cursor, o_code, o_message }, _connectionString);

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

        public async Task<CResponseMessage> Add(AddEpisodeAsset dto)
        {
            try
            {
                // IN
                var p_episode_id = new OracleParameter("p_episode_id", OracleDbType.Decimal, dto.EpisodeId, ParameterDirection.Input);
                var p_asset_type = new OracleParameter("p_asset_type", OracleDbType.Varchar2, dto.AssetType, ParameterDirection.Input);
                var p_url = new OracleParameter("p_url", OracleDbType.Varchar2, dto.Url, ParameterDirection.Input);
                var p_sort_order = new OracleParameter("p_sort_order", OracleDbType.Int32, dto.SortOrder, ParameterDirection.Input);

                // OUT
                var o_asset_id = new OracleParameter("o_asset_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_episode_id, p_asset_type, p_url, p_sort_order,
                    o_asset_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_asset_add", parameters, _connectionString);

                // ✅ Đọc OUT NUMBER đúng cách
                decimal? newAssetId = null;
                if (o_asset_id.Value != null && o_asset_id.Value != DBNull.Value)
                {
                    var od = (OracleDecimal)o_asset_id.Value;
                    if (!od.IsNull) newAssetId = od.Value;
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

        public async Task<CResponseMessage> Update_episode(UpdateEpisodeAsset dto)
        {
            try
            {
                // IN
                var p_asset_id = new OracleParameter("p_asset_id", OracleDbType.Decimal, dto.AssetId, ParameterDirection.Input);
                var p_asset_type = new OracleParameter("p_asset_type", OracleDbType.Varchar2, dto.AssetType, ParameterDirection.Input);
                var p_url = new OracleParameter("p_url", OracleDbType.Varchar2, dto.Url, ParameterDirection.Input);
                var p_sort_order = new OracleParameter("p_sort_order", OracleDbType.Int32, dto.SortOrder, ParameterDirection.Input);

                // OUT
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[]
                {
                    p_asset_id, p_asset_type, p_url, p_sort_order,
                    o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_asset_update", parameters, _connectionString);

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
                var p_asset_id = new OracleParameter("p_asset_id", OracleDbType.Decimal, assetId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var parameters = new OracleParameter[] { p_asset_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_episode_asset_delete", parameters, _connectionString);

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
