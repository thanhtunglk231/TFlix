SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Description:   Đăng ký tài khoản người dùng mới trên TFlix
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_register_user
    @p_email     NVARCHAR(320),
    @p_full_name NVARCHAR(200),
    @p_password  NVARCHAR(500),
    @o_code      NVARCHAR(10) OUTPUT,
    @o_message   NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @p_email IS NULL OR LTRIM(RTRIM(@p_email)) = ''
        BEGIN
            SET @o_code = N'400';
            SET @o_message = N'Email không được để trống.';
            RETURN;
        END;

        IF @p_password IS NULL OR LTRIM(RTRIM(@p_password)) = ''
        BEGIN
            SET @o_code = N'400';
            SET @o_message = N'Mật khẩu không được để trống.';
            RETURN;
        END;

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

        IF EXISTS (SELECT 1 FROM dbo.app_users WHERE LOWER(email) = LOWER(@p_email))
        BEGIN
            SET @o_code = N'400';
            SET @o_message = N'Email này đã được đăng ký tài khoản.';
            RETURN;
        END;

        -- Xác định cột mật khẩu
        DECLARE @pass_col NVARCHAR(50) = N'password_hash';
        IF COL_LENGTH(N'dbo.app_users', N'password_hash') IS NULL AND COL_LENGTH(N'dbo.app_users', N'password') IS NOT NULL
            SET @pass_col = N'password';

        DECLARE @name NVARCHAR(200) = COALESCE(NULLIF(LTRIM(RTRIM(@p_full_name)), ''), SUBSTRING(@p_email, 1, CHARINDEX('@', @p_email) - 1), N'Thành viên mới');

        DECLARE @ins_sql NVARCHAR(MAX) = N'
            INSERT INTO dbo.app_users (email, ' + @pass_col + N', full_name, avatar_url, status, is_email_verified)
            VALUES (@email, @pass, @fullname, N''https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=120&auto=format&fit=crop'', N''ACTIVE'', ''Y'');
        ';

        EXEC sp_executesql @ins_sql,
            N'@email NVARCHAR(320), @pass NVARCHAR(500), @fullname NVARCHAR(200)',
            @email = @p_email,
            @pass = @p_password,
            @fullname = @name;

        SET @o_code = N'200';
        SET @o_message = N'Đăng ký tài khoản thành công.';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
