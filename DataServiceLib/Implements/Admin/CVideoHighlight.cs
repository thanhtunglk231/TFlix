using CoreLib.Dtos.VideoHighlight;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements.Admin;

public sealed class CVideoHighlight : ICVideoHighlight
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;

    public CVideoHighlight(ICBaseProvider baseProvider, IConfiguration configuration)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer")!;
    }

    public Task<CResponseMessage> GetAllAsync(int page = 1, int pageSize = 10, bool activeOnly = false)
        => ExecuteAsync("GET_ALL", page: page, pageSize: pageSize, activeOnly: activeOnly);
    public Task<CResponseMessage> GetBySourceAsync(long sourceId)
        => ExecuteAsync("GET_BY_SOURCE", sourceId: sourceId);
    public Task<CResponseMessage> AddAsync(CreateVideoHighlightDto dto)
        => ExecuteAsync("ADD", sourceId: dto.SourceId, title: dto.Title, description: dto.Description,
            startSeconds: dto.StartSeconds, endSeconds: dto.EndSeconds, videoUrl: dto.VideoUrl,
            thumbnailUrl: dto.ThumbnailUrl, sortOrder: dto.SortOrder);
    public Task<CResponseMessage> DeleteAsync(long highlightId)
        => ExecuteAsync("DELETE", highlightId: highlightId);

    private Task<CResponseMessage> ExecuteAsync(string action, long? highlightId = null, long? sourceId = null,
        string? title = null, string? description = null, decimal? startSeconds = null, decimal? endSeconds = null,
        string? videoUrl = null, string? thumbnailUrl = null, int? sortOrder = null, int page = 1,
        int pageSize = 10, bool activeOnly = false)
    {
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var parameters = new IDbDataParameter[]
            {
                new SqlParameter("@p_action", SqlDbType.NVarChar, 30) { Value = action },
                new SqlParameter("@p_highlight_id", SqlDbType.BigInt) { Value = (object?)highlightId ?? DBNull.Value },
                new SqlParameter("@p_source_id", SqlDbType.BigInt) { Value = (object?)sourceId ?? DBNull.Value },
                new SqlParameter("@p_title", SqlDbType.NVarChar, 250) { Value = (object?)title ?? DBNull.Value },
                new SqlParameter("@p_description", SqlDbType.NVarChar, 1000) { Value = (object?)description ?? DBNull.Value },
                new SqlParameter("@p_start_seconds", SqlDbType.Decimal) { Precision = 12, Scale = 3, Value = (object?)startSeconds ?? DBNull.Value },
                new SqlParameter("@p_end_seconds", SqlDbType.Decimal) { Precision = 12, Scale = 3, Value = (object?)endSeconds ?? DBNull.Value },
                new SqlParameter("@p_video_url", SqlDbType.NVarChar, 1600) { Value = (object?)videoUrl ?? DBNull.Value },
                new SqlParameter("@p_thumbnail_url", SqlDbType.NVarChar, 1600) { Value = (object?)thumbnailUrl ?? DBNull.Value },
                new SqlParameter("@p_sort_order", SqlDbType.Int) { Value = (object?)sortOrder ?? DBNull.Value },
                new SqlParameter("@p_page", SqlDbType.Int) { Value = Math.Max(page, 1) },
                new SqlParameter("@p_page_size", SqlDbType.Int) { Value = Math.Clamp(pageSize, 1, 100) },
                new SqlParameter("@p_active_only", SqlDbType.Bit) { Value = activeOnly }, code, message
            };
            var data = _baseProvider.GetDatasetFromSP("sp_video_highlight_management", parameters, _connectionString);
            var responseCode = code.Value?.ToString() ?? "500";
            return Task.FromResult(new CResponseMessage { Success = responseCode == "200", code = responseCode,
                message = message.Value?.ToString() ?? "Không nhận được phản hồi.", Data = data });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Lỗi highlight: " + ex.Message });
        }
    }
}
