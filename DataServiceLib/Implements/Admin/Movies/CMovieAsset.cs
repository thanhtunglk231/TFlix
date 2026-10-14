using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovieAsset : ICMovieAsset
    {
        private const string ProcedureName = "sp_admin_media_asset_management";
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovieAsset(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer")!;
        }

        public Task<CResponseMessage> sp_get_all(int pageSize = 25, string? cursorOwnerType = null, long? cursorAssetId = null, string? ownerType = null)
            => ExecuteAsync("GET_ALL", ownerType, pageSize: pageSize, cursorOwnerType: cursorOwnerType, cursorAssetId: cursorAssetId);
        public Task<CResponseMessage> GetOwners() => ExecuteAsync("GET_OWNERS");
        public async Task<CResponseMessage> GetByOwner(long ownerId, string ownerType)
        {
            var normalizedOwnerType = ownerType.Trim().ToUpperInvariant();
            var (tableName, ownerColumn) = normalizedOwnerType switch
            {
                "MOVIE" => ("dbo.movie_assets", "movie_id"),
                "SERIES" => ("dbo.series_assets", "series_id"),
                "EPISODE" => ("dbo.episode_assets", "episode_id"),
                _ => (string.Empty, string.Empty)
            };

            if (ownerId <= 0 || string.IsNullOrEmpty(tableName))
                return new CResponseMessage { Success = false, code = "400", message = "Loại nội dung hoặc mã nội dung không hợp lệ." };

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"""
                    SELECT asset_id AS AssetId, @ownerType AS OwnerType, {ownerColumn} AS OwnerId,
                           asset_type AS AssetType, url AS Url, sort_order AS SortOrder
                    FROM {tableName}
                    WHERE {ownerColumn} = @ownerId
                    ORDER BY sort_order, asset_id;
                    """;
                command.Parameters.Add(new SqlParameter("@ownerType", SqlDbType.NVarChar, 20) { Value = normalizedOwnerType });
                command.Parameters.Add(new SqlParameter("@ownerId", SqlDbType.BigInt) { Value = ownerId });

                await using var reader = await command.ExecuteReaderAsync();
                var table = new DataTable("Table");
                table.Load(reader);
                var dataSet = new DataSet();
                dataSet.Tables.Add(table);
                return new CResponseMessage
                {
                    Success = true,
                    code = "200",
                    message = "Lấy danh sách tài nguyên thành công.",
                    Data = dataSet
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi tải tài nguyên: " + ex.Message };
            }
        }
        public Task<CResponseMessage> sp_get_by_id(decimal id, string ownerType = "MOVIE")
            => ExecuteAsync("GET_BY_ID", ownerType, assetId: id);
        public Task<CResponseMessage> Add(AddMovieAssetDto dto)
            => ExecuteAsync("ADD", dto.OwnerType, dto.OwnerId, null, dto.AssetType, dto.Url, Convert.ToInt32(dto.SortOrder));
        public Task<CResponseMessage> Update_episode(UpdateMovieAssetDto dto)
            => ExecuteAsync("UPDATE", dto.OwnerType, dto.OwnerId, dto.AssetId, dto.AssetType, dto.Url, dto.SortOrder);
        public Task<CResponseMessage> Delete_episode(decimal assetId, string ownerType = "MOVIE")
            => ExecuteAsync("DELETE", ownerType, assetId: assetId);

        private Task<CResponseMessage> ExecuteAsync(string action, string? ownerType = null, long? ownerId = null,
            decimal? assetId = null, string? assetType = null, string? url = null, int? sortOrder = null,
            int? pageSize = null, string? cursorOwnerType = null, long? cursorAssetId = null)
        {
            try
            {
                var outputAssetId = new SqlParameter("@o_asset_id", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
                var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[]
                {
                    new SqlParameter("@p_action", SqlDbType.NVarChar, 20) { Value = action },
                    new SqlParameter("@p_owner_type", SqlDbType.NVarChar, 20) { Value = (object?)ownerType ?? DBNull.Value },
                    new SqlParameter("@p_owner_id", SqlDbType.BigInt) { Value = (object?)ownerId ?? DBNull.Value },
                    new SqlParameter("@p_asset_id", SqlDbType.BigInt) { Value = (object?)assetId ?? DBNull.Value },
                    new SqlParameter("@p_asset_type", SqlDbType.NVarChar, 30) { Value = (object?)assetType ?? DBNull.Value },
                    new SqlParameter("@p_url", SqlDbType.NVarChar, 1600) { Value = (object?)url ?? DBNull.Value },
                    new SqlParameter("@p_sort_order", SqlDbType.Int) { Value = (object?)sortOrder ?? DBNull.Value },
                    new SqlParameter("@p_page_size", SqlDbType.Int) { Value = (object?)pageSize ?? DBNull.Value },
                    new SqlParameter("@p_cursor_owner_type", SqlDbType.NVarChar, 20) { Value = (object?)cursorOwnerType ?? DBNull.Value },
                    new SqlParameter("@p_cursor_asset_id", SqlDbType.BigInt) { Value = (object?)cursorAssetId ?? DBNull.Value },
                    outputAssetId, code, message
                };

                var dataSet = _baseProvider.GetDatasetFromSP(ProcedureName, parameters, _connectionString);
                var responseCode = code.Value?.ToString() ?? "500";
                return Task.FromResult(new CResponseMessage
                {
                    Success = responseCode == "200",
                    code = responseCode,
                    message = message.Value?.ToString() ?? "Không nhận được phản hồi.",
                    Data = dataSet
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Lỗi quản lý media asset: " + ex.Message });
            }
        }
    }
}
