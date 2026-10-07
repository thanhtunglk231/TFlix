using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CAuth : ICAuth
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CAuth(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> LoginAsync(LoginDto loginDto)
        {
            try
            {
                await EnsureStoredProceduresAndTablesExistAsync();

                var p_email = new SqlParameter("@p_email", SqlDbType.NVarChar, 320)
                {
                    Direction = ParameterDirection.Input,
                    Value = (object?)loginDto.Username?.Trim() ?? DBNull.Value
                };
                var p_password = new SqlParameter("@p_password", SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Input,
                    Value = (object?)loginDto.Password ?? DBNull.Value
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { p_email, p_password, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_login_user", parameters, _connectionString);

                string code = o_code.Value?.ToString() ?? "";
                string message = o_message.Value?.ToString() ?? "";

                if (string.Equals(code, "200", StringComparison.Ordinal) && dataset != null && dataset.Tables.Count > 0 && dataset.Tables[0].Rows.Count > 0)
                {
                    return new CResponseMessage
                    {
                        Data = dataset,
                        code = "200",
                        message = string.IsNullOrWhiteSpace(message) ? "Đăng nhập thành công." : message,
                        Success = true
                    };
                }

                // Nếu SP trả mã lỗi cụ thể (sai mật khẩu 401, không tìm thấy 404, khóa 403)
                if (!string.IsNullOrEmpty(code) && code != "500")
                {
                    return new CResponseMessage
                    {
                        Data = null,
                        code = code,
                        message = string.IsNullOrWhiteSpace(message) ? "Đăng nhập thất bại." : message,
                        Success = false
                    };
                }

                return new CResponseMessage
                {
                    Data = null,
                    code = "500",
                    message = string.IsNullOrWhiteSpace(message) ? "Không thể xác thực tài khoản." : message,
                    Success = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CAuth.LoginAsync] Exception: " + ex.Message);
                return new CResponseMessage
                {
                    Data = null,
                    code = "500",
                    message = "Không thể kết nối dịch vụ xác thực. Vui lòng thử lại sau.",
                    Success = false
                };
            }
        }

        public async Task<CResponseMessage> Register(RegisterDto registerDto)
        {
            try
            {
                await EnsureStoredProceduresAndTablesExistAsync();

                var p_email = new SqlParameter("@p_email", SqlDbType.NVarChar, 320)
                {
                    Direction = ParameterDirection.Input,
                    Value = registerDto.Email?.Trim()
                };
                var p_fullname = new SqlParameter("@p_full_name", SqlDbType.NVarChar, 200)
                {
                    Direction = ParameterDirection.Input,
                    Value = (object?)registerDto.FullName?.Trim() ?? DBNull.Value
                };
                var p_password = new SqlParameter("@p_password", SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Input,
                    Value = registerDto.Password
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { p_email, p_fullname, p_password, o_code, o_message };

                var ds = _baseProvider.GetDatasetFromSP("sp_register_user", parameters, _connectionString);

                string code = o_code.Value?.ToString() ?? "500";
                string message = o_message.Value?.ToString() ?? "Không thể đăng ký tài khoản.";

                return new CResponseMessage
                {
                    Data = ds,
                    code = code,
                    message = message,
                    Success = (code == "200")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CAuth.Register] Exception: " + ex.Message);
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Không thể kết nối dịch vụ đăng ký. Vui lòng thử lại sau."
                };
            }
        }

        private async Task EnsureStoredProceduresAndTablesExistAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sqlScript = @"
                    IF OBJECT_ID(N'dbo.app_users', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.app_users
                        (
                            user_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_app_users PRIMARY KEY,
                            email NVARCHAR(320) NOT NULL CONSTRAINT UQ_app_users_email UNIQUE,
                            password_hash NVARCHAR(500) NOT NULL,
                            full_name NVARCHAR(200) NOT NULL,
                            avatar_url NVARCHAR(1000) NULL,
                            phone NVARCHAR(50) NULL,
                            country_code NVARCHAR(10) NULL CONSTRAINT DF_app_users_country DEFAULT N'VN',
                            language_code NVARCHAR(10) NULL CONSTRAINT DF_app_users_lang DEFAULT N'vi',
                            is_email_verified CHAR(1) NOT NULL CONSTRAINT DF_app_users_email_verified DEFAULT 'Y',
                            status NVARCHAR(50) NOT NULL CONSTRAINT DF_app_users_status DEFAULT N'ACTIVE',
                            created_at DATETIMEOFFSET NOT NULL CONSTRAINT DF_app_users_created_at DEFAULT SYSDATETIMEOFFSET(),
                            updated_at DATETIMEOFFSET NULL
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.app_users WHERE email = N'thanhtung230323@gmail.com')
                    BEGIN
                        INSERT INTO dbo.app_users (email, password_hash, full_name, avatar_url, phone, status, is_email_verified)
                        VALUES (N'thanhtung230323@gmail.com', N'string', N'Thanh Tùng', N'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=120&auto=format&fit=crop', N'0987654321', N'ACTIVE', 'Y');
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.app_users WHERE email = N'admin@tflix.com')
                    BEGIN
                        INSERT INTO dbo.app_users (email, password_hash, full_name, avatar_url, phone, status, is_email_verified)
                        VALUES (N'admin@tflix.com', N'admin123', N'Quản trị viên TFlix', N'https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=120&auto=format&fit=crop', N'0909000999', N'ACTIVE', 'Y');
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_login_user]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE dbo.sp_login_user
                            @p_email    NVARCHAR(320),
                            @p_password NVARCHAR(500),
                            @o_code     NVARCHAR(10) OUTPUT,
                            @o_message  NVARCHAR(4000) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            BEGIN TRY
                                IF @p_email IS NULL OR LTRIM(RTRIM(@p_email)) = ''''
                                BEGIN
                                    SET @o_code = N''400'';
                                    SET @o_message = N''Email không được để trống.'';
                                    RETURN;
                                END;

                                DECLARE @user_id BIGINT;
                                DECLARE @stored_hash NVARCHAR(500);
                                DECLARE @status NVARCHAR(50);
                                DECLARE @full_name NVARCHAR(200);

                                DECLARE @pass_col NVARCHAR(50) = N''password_hash'';
                                IF COL_LENGTH(N''dbo.app_users'', N''password_hash'') IS NULL AND COL_LENGTH(N''dbo.app_users'', N''password'') IS NOT NULL
                                    SET @pass_col = N''password'';

                                DECLARE @check_sql NVARCHAR(MAX) = N''
                                    SELECT TOP 1 
                                        @user_id = user_id, 
                                        @stored_hash = '' + @pass_col + N'', 
                                        @status = status,
                                        @full_name = full_name
                                    FROM dbo.app_users
                                    WHERE LOWER(email) = LOWER(@email);
                                '';

                                EXEC sp_executesql @check_sql,
                                    N''@email NVARCHAR(320), @user_id BIGINT OUTPUT, @stored_hash NVARCHAR(500) OUTPUT, @status NVARCHAR(50) OUTPUT, @full_name NVARCHAR(200) OUTPUT'',
                                    @email = @p_email,
                                    @user_id = @user_id OUTPUT,
                                    @stored_hash = @stored_hash OUTPUT,
                                    @status = @status OUTPUT,
                                    @full_name = @full_name OUTPUT;

                                IF @user_id IS NULL
                                BEGIN
                                    SET @o_code = N''404'';
                                    SET @o_message = N''Tài khoản không tồn tại trên hệ thống.'';
                                    RETURN;
                                END;

                                IF @status IS NOT NULL AND @status <> N''ACTIVE''
                                BEGIN
                                    SET @o_code = N''403'';
                                    SET @o_message = N''Tài khoản đã bị tạm khóa.'';
                                    RETURN;
                                END;

                                IF @stored_hash <> @p_password
                                   AND @stored_hash <> CONVERT(NVARCHAR(500), HASHBYTES(''SHA2_256'', @p_password), 2)
                                   AND @stored_hash <> CONVERT(NVARCHAR(500), HASHBYTES(''MD5'', @p_password), 2)
                                BEGIN
                                    SET @o_code = N''401'';
                                    SET @o_message = N''Mật khẩu đăng nhập không chính xác.'';
                                    RETURN;
                                END;

                                SELECT 
                                    u.user_id        AS USER_ID,
                                    u.email          AS EMAIL,
                                    u.full_name      AS FULL_NAME,
                                    COALESCE(u.avatar_url, N''https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=120&auto=format&fit=crop'') AS AVATAR_URL,
                                    ISNULL(u.phone, N'''') AS PHONE,
                                    COALESCE(u.country_code, N''VN'') AS COUNTRY_CODE,
                                    COALESCE(u.language_code, N''vi'') AS LANGUAGE_CODE,
                                    ISNULL(u.is_email_verified, ''Y'') AS IS_EMAIL_VERIFIED,
                                    ISNULL(u.status, N''ACTIVE'') AS STATUS,
                                    ISNULL((
                                        SELECT STRING_AGG(r.role_code, '','')
                                        FROM dbo.user_roles ur
                                        INNER JOIN dbo.roles r ON r.role_id = ur.role_id
                                        WHERE ur.user_id = u.user_id
                                    ), N'''') AS ROLES
                                FROM dbo.app_users u
                                WHERE u.user_id = @user_id;

                                SET @o_code = N''200'';
                                SET @o_message = N''Đăng nhập thành công.'';
                            END TRY
                            BEGIN CATCH
                                SET @o_code = N''500'';
                                SET @o_message = ERROR_MESSAGE();
                            END CATCH
                        END;
                        ')
                    END;
                ";

                using var cmd = new SqlCommand(sqlScript, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CAuth.EnsureStoredProceduresAndTablesExistAsync] Error: " + ex.Message);
            }
        }

        public async Task<CResponseMessage> IssueOtpAsync(string email, string purpose, string otpHash)
        {
            return await ExecuteOtpProcedureAsync("sp_auth_otp_issue", email, purpose, otpHash);
        }

        public async Task<CResponseMessage> VerifyOtpAsync(string email, string purpose, string otpHash)
        {
            return await ExecuteOtpProcedureAsync("sp_auth_otp_verify", email, purpose, otpHash);
        }

        private async Task<CResponseMessage> ExecuteOtpProcedureAsync(string procedure, string email, string purpose, string otpHash)
        {
            try
            {
                var pEmail = new SqlParameter("@p_email", SqlDbType.NVarChar, 320) { Value = email };
                var pPurpose = new SqlParameter("@p_purpose", SqlDbType.NVarChar, 30) { Value = purpose };
                var pHash = new SqlParameter("@p_otp_hash", SqlDbType.NVarChar, 128) { Value = otpHash };
                var oCode = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var oMessage = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
                var parameters = new IDbDataParameter[] { pEmail, pPurpose, pHash, oCode, oMessage };
                var data = _baseProvider.GetDatasetFromSP(procedure, parameters, _connectionString);
                var code = oCode.Value?.ToString() ?? "500";
                return new CResponseMessage { Success = code == "200", code = code, message = oMessage.Value?.ToString(), Data = data };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CAuth.{procedure}] Exception: {ex.Message}");
                return new CResponseMessage { Success = false, code = "500", message = "Không thể xử lý OTP. Vui lòng thử lại sau." };
            }
        }
    }
}
