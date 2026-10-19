using System.Data;
using System.Diagnostics;
using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataServiceLib.Implements.Admin;

public sealed class CVideoUploadRequest : ICVideoUploadRequest
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;
    private readonly ILogger<CVideoUploadRequest> _logger;
    private static bool _initialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public CVideoUploadRequest(ICBaseProvider baseProvider, IConfiguration configuration, ILogger<CVideoUploadRequest> logger)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing SqlServer connection string.");
        _logger = logger;
    }

    private async Task EnsureTableAndProceduresAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            const string ddlSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'video_upload_requests')
BEGIN
    CREATE TABLE video_upload_requests (
        request_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        user_id BIGINT NOT NULL,
        user_name NVARCHAR(255) NULL,
        user_email NVARCHAR(255) NULL,
        plan_name NVARCHAR(100) NULL,
        reason NVARCHAR(1000) NULL,
        status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
        admin_notes NVARCHAR(1000) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        reviewed_at DATETIME2 NULL,
        reviewed_by BIGINT NULL
    );
    CREATE INDEX IX_video_upload_requests_user ON video_upload_requests(user_id);
    CREATE INDEX IX_video_upload_requests_status ON video_upload_requests(status);
END;
";
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(ddlSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Kiểm tra và tạo 4 Stored Procedure nếu chưa tồn tại
                var scripts = new (string Name, string Sql)[]
                {
                    ("sp_video_upload_request_submit", @"
CREATE OR ALTER PROCEDURE sp_video_upload_request_submit
    @p_user_id BIGINT,
    @p_user_name NVARCHAR(255) = NULL,
    @p_user_email NVARCHAR(255) = NULL,
    @p_plan_name NVARCHAR(100) = NULL,
    @p_reason NVARCHAR(1000) = NULL,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF (@p_user_id <= 0)
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Thông tin người dùng không hợp lệ.';
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM video_upload_requests WHERE user_id = @p_user_id AND status = 'PENDING')
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Bạn đã gửi yêu cầu trước đó và đang chờ quản trị viên xét duyệt.';
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM video_upload_requests WHERE user_id = @p_user_id AND status = 'APPROVED')
        BEGIN
            SET @o_code = '200';
            SET @o_message = N'Tài khoản của bạn đã được phê duyệt quyền đăng tải video trước đó.';
            RETURN;
        END

        INSERT INTO video_upload_requests (
            user_id, user_name, user_email, plan_name, reason, status, created_at
        ) VALUES (
            @p_user_id, @p_user_name, @p_user_email, @p_plan_name, @p_reason, 'PENDING', GETDATE()
        );

        SET @o_code = '200';
        SET @o_message = N'Gửi yêu cầu tải lên video thành công! Quản trị viên sẽ sớm xem xét yêu cầu của bạn.';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;"),
                    ("sp_video_upload_request_get_user_status", @"
CREATE OR ALTER PROCEDURE sp_video_upload_request_get_user_status
    @p_user_id BIGINT,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT TOP 1
            request_id AS RequestId,
            user_id AS UserId,
            user_name AS UserName,
            user_email AS UserEmail,
            plan_name AS PlanName,
            reason AS Reason,
            status AS Status,
            admin_notes AS AdminNotes,
            created_at AS CreatedAt,
            reviewed_at AS ReviewedAt,
            reviewed_by AS ReviewedBy
        FROM video_upload_requests
        WHERE user_id = @p_user_id
        ORDER BY created_at DESC;

        SET @o_code = '200';
        SET @o_message = N'Lấy trạng thái thành công.';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;"),
                    ("sp_video_upload_request_get_all", @"
CREATE OR ALTER PROCEDURE sp_video_upload_request_get_all
    @p_status NVARCHAR(50) = NULL,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            request_id AS RequestId,
            user_id AS UserId,
            user_name AS UserName,
            user_email AS UserEmail,
            plan_name AS PlanName,
            reason AS Reason,
            status AS Status,
            admin_notes AS AdminNotes,
            created_at AS CreatedAt,
            reviewed_at AS ReviewedAt,
            reviewed_by AS ReviewedBy
        FROM video_upload_requests
        WHERE (@p_status IS NULL OR @p_status = '' OR status = @p_status)
        ORDER BY 
            CASE WHEN status = 'PENDING' THEN 0 ELSE 1 END,
            created_at DESC;

        SET @o_code = '200';
        SET @o_message = N'Lấy danh sách yêu cầu thành công.';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;"),
                    ("sp_video_upload_request_review", @"
CREATE OR ALTER PROCEDURE sp_video_upload_request_review
    @p_request_id BIGINT,
    @p_status NVARCHAR(50),
    @p_admin_notes NVARCHAR(1000) = NULL,
    @p_reviewed_by BIGINT = NULL,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF (@p_request_id <= 0)
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Yêu cầu không hợp lệ.';
            RETURN;
        END

        IF (@p_status NOT IN ('APPROVED', 'REJECTED', 'PENDING'))
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Trạng thái phê duyệt không hợp lệ.';
            RETURN;
        END

        UPDATE video_upload_requests
        SET status = @p_status,
            admin_notes = @p_admin_notes,
            reviewed_at = GETDATE(),
            reviewed_by = @p_reviewed_by
        WHERE request_id = @p_request_id;

        IF @@ROWCOUNT = 0
        BEGIN
            SET @o_code = '404';
            SET @o_message = N'Không tìm thấy yêu cầu cần cập nhật.';
            RETURN;
        END

        SET @o_code = '200';
        SET @o_message = N'Đã cập nhật trạng thái yêu cầu thành công.';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;")
                };

                foreach (var item in scripts)
                {
                    using var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM sys.procedures WHERE name = '{item.Name}'", conn);
                    var count = (int?)await checkCmd.ExecuteScalarAsync() ?? 0;
                    if (count == 0)
                    {
                        using var createCmd = new SqlCommand(item.Sql, conn);
                        await createCmd.ExecuteNonQueryAsync();
                        _logger.LogInformation("Auto-created procedure {SpName} on current database.", item.Name);
                    }
                }
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure video_upload_requests table exists: {Message}", ex.Message);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<CResponseMessage> SubmitRequestAsync(CreateVideoUploadRequestDto dto)
    {
        await EnsureTableAndProceduresAsync();
        var parameters = new List<IDbDataParameter>
        {
            new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = dto.UserId },
            new SqlParameter("@p_user_name", SqlDbType.NVarChar, 255) { Value = (object?)dto.UserName ?? DBNull.Value },
            new SqlParameter("@p_user_email", SqlDbType.NVarChar, 255) { Value = (object?)dto.UserEmail ?? DBNull.Value },
            new SqlParameter("@p_plan_name", SqlDbType.NVarChar, 100) { Value = (object?)dto.PlanName ?? DBNull.Value },
            new SqlParameter("@p_reason", SqlDbType.NVarChar, 1000) { Value = (object?)dto.Reason ?? DBNull.Value }
        };

        return await ExecuteAsync("sp_video_upload_request_submit", parameters);
    }

    public async Task<CResponseMessage> GetUserStatusAsync(long userId)
    {
        await EnsureTableAndProceduresAsync();
        var parameters = new List<IDbDataParameter>
        {
            new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = userId }
        };

        return await ExecuteAsync("sp_video_upload_request_get_user_status", parameters);
    }

    public async Task<CResponseMessage> GetAllAsync(string? status)
    {
        await EnsureTableAndProceduresAsync();
        var parameters = new List<IDbDataParameter>
        {
            new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim().ToUpperInvariant() }
        };

        return await ExecuteAsync("sp_video_upload_request_get_all", parameters);
    }

    public async Task<CResponseMessage> ReviewRequestAsync(ReviewVideoUploadRequestDto dto)
    {
        await EnsureTableAndProceduresAsync();
        var parameters = new List<IDbDataParameter>
        {
            new SqlParameter("@p_request_id", SqlDbType.BigInt) { Value = dto.RequestId },
            new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Value = dto.Status.Trim().ToUpperInvariant() },
            new SqlParameter("@p_admin_notes", SqlDbType.NVarChar, 1000) { Value = (object?)dto.AdminNotes ?? DBNull.Value },
            new SqlParameter("@p_reviewed_by", SqlDbType.BigInt) { Value = (object?)dto.AdminUserId ?? DBNull.Value }
        };

        return await ExecuteAsync("sp_video_upload_request_review", parameters);
    }

    private Task<CResponseMessage> ExecuteAsync(string procedure, IEnumerable<IDbDataParameter> input)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var data = _baseProvider.GetDatasetFromSP(procedure, input.Concat([code, message]).ToArray(), _connectionString);
            var resultCode = code.Value?.ToString() ?? "500";
            var result = new CResponseMessage
            {
                Success = resultCode == "200",
                code = resultCode,
                message = message.Value?.ToString() ?? "Không xử lý được yêu cầu.",
                Data = data
            };

            _logger.Log(result.Success ? LogLevel.Information : LogLevel.Warning,
                "Video upload request procedure {Procedure} returned {ResultCode} in {ElapsedMilliseconds} ms: {ResultMessage}",
                procedure, result.code, stopwatch.ElapsedMilliseconds, result.message);
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Video upload request procedure {Procedure} failed", procedure);
            return Task.FromResult(new CResponseMessage { Success = false, code = "500", message = "Lỗi xử lý yêu cầu tải video." });
        }
    }
}
