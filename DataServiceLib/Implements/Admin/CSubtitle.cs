using CoreLib.Dtos.Subtitle;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements.Admin;

public sealed class CSubtitle : ICSubtitle
{
    private readonly ICBaseProvider _provider;
    private readonly string _connectionString;
    public CSubtitle(ICBaseProvider provider, IConfiguration configuration)
    { _provider = provider; _connectionString = configuration.GetConnectionString("SqlServer")!; }

    public Task<CResponseMessage> GetBySourceAsync(long sourceId) => ExecuteAsync("GET_BY_SOURCE", sourceId);
    public Task<CResponseMessage> UpsertAsync(CreateSubtitleDto dto) =>
        ExecuteAsync("UPSERT", dto.SourceId, dto.LanguageCode, dto.Format, dto.Url, dto.IsDefault);

    private Task<CResponseMessage> ExecuteAsync(string action, long sourceId, string? languageCode = null,
        string? format = null, string? url = null, bool isDefault = false)
    {
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var parameters = new IDbDataParameter[]
            {
                new SqlParameter("@p_action", SqlDbType.NVarChar, 30) { Value = action },
                new SqlParameter("@p_source_id", SqlDbType.BigInt) { Value = sourceId },
                new SqlParameter("@p_language_code", SqlDbType.NVarChar, 10) { Value = (object?)languageCode ?? DBNull.Value },
                new SqlParameter("@p_format", SqlDbType.NVarChar, 10) { Value = (object?)format ?? DBNull.Value },
                new SqlParameter("@p_url", SqlDbType.NVarChar, 1000) { Value = (object?)url ?? DBNull.Value },
                new SqlParameter("@p_is_default", SqlDbType.Bit) { Value = isDefault }, code, message
            };
            var data = _provider.GetDatasetFromSP("sp_subtitle_management", parameters, _connectionString);
            var responseCode = code.Value?.ToString() ?? "500";
            return Task.FromResult(new CResponseMessage { Success = responseCode == "200", code = responseCode,
                message = message.Value?.ToString() ?? "Không nhận được phản hồi.", Data = data });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Lỗi phụ đề: " + ex.Message });
        }
    }
}
