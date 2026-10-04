SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Description:   Thêm mới bình luận (tự động tương thích với mọi cấu trúc bảng comments)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Comment_Add
    @p_movie_id   BIGINT = NULL,
    @p_episode_id BIGINT = NULL,
    @p_user_id    BIGINT,
    @p_user_name  NVARCHAR(255) = NULL,
    @p_avatar_url NVARCHAR(500) = NULL,
    @p_content    NVARCHAR(MAX),
    @p_parent_id  BIGINT = NULL,
    @o_comment_id BIGINT OUTPUT,
    @o_code       NVARCHAR(10) OUTPUT,
    @o_message    NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @p_content IS NULL OR LTRIM(RTRIM(@p_content)) = ''
        BEGIN
            SET @o_comment_id = 0;
            SET @o_code = N'400';
            SET @o_message = N'Nội dung bình luận không được để trống';
            RETURN;
        END;

        -- Đảm bảo bảng comments tồn tại
        IF OBJECT_ID(N'dbo.comments', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.comments (
                comment_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
                movie_id     BIGINT NULL,
                episode_id   BIGINT NULL,
                user_id      BIGINT NOT NULL,
                content      NVARCHAR(MAX) NOT NULL,
                status       NVARCHAR(50) DEFAULT 'ACTIVE',
                parent_id    BIGINT NULL,
                created_at   DATETIME DEFAULT GETDATE()
            );

            CREATE INDEX IX_comments_movie ON dbo.comments(movie_id);
            CREATE INDEX IX_comments_episode ON dbo.comments(episode_id);
        END;

        -- 1. Xác định tên cột chứa nội dung bình luận
        DECLARE @content_col NVARCHAR(100) = N'content';
        IF COL_LENGTH(N'dbo.comments', N'content') IS NOT NULL
            SET @content_col = N'content';
        ELSE IF COL_LENGTH(N'dbo.comments', N'comment_text') IS NOT NULL
            SET @content_col = N'comment_text';
        ELSE IF COL_LENGTH(N'dbo.comments', N'comment') IS NOT NULL
            SET @content_col = N'comment';
        ELSE IF COL_LENGTH(N'dbo.comments', N'body') IS NOT NULL
            SET @content_col = N'body';

        -- 2. Xây dựng danh sách các cột chèn động
        DECLARE @cols NVARCHAR(MAX) = N'user_id, ' + QUOTENAME(@content_col);
        DECLARE @vals NVARCHAR(MAX) = N'@user_id, @content';

        IF COL_LENGTH(N'dbo.comments', N'movie_id') IS NOT NULL
        BEGIN
            SET @cols += N', movie_id';
            SET @vals += N', @movie_id';
        END;

        IF COL_LENGTH(N'dbo.comments', N'episode_id') IS NOT NULL
        BEGIN
            SET @cols += N', episode_id';
            SET @vals += N', @episode_id';
        END;

        IF COL_LENGTH(N'dbo.comments', N'parent_id') IS NOT NULL
        BEGIN
            SET @cols += N', parent_id';
            SET @vals += N', @parent_id';
        END;

        IF COL_LENGTH(N'dbo.comments', N'status') IS NOT NULL
        BEGIN
            SET @cols += N', status';
            SET @vals += N', ''ACTIVE''';
        END;

        IF COL_LENGTH(N'dbo.comments', N'created_at') IS NOT NULL
        BEGIN
            SET @cols += N', created_at';
            SET @vals += N', GETDATE()';
        END;

        -- Nếu bảng có lưu user_name trực tiếp
        IF COL_LENGTH(N'dbo.comments', N'user_name') IS NOT NULL
        BEGIN
            SET @cols += N', user_name';
            SET @vals += N', @user_name';
        END;

        -- Nếu bảng có lưu avatar_url trực tiếp
        IF COL_LENGTH(N'dbo.comments', N'avatar_url') IS NOT NULL
        BEGIN
            SET @cols += N', avatar_url';
            SET @vals += N', @avatar_url';
        END;

        -- 3. Thực thi chèn dữ liệu an toàn
        DECLARE @sql NVARCHAR(MAX) = N'
            INSERT INTO dbo.comments (' + @cols + N')
            VALUES (' + @vals + N');
            SELECT @new_id = CAST(SCOPE_IDENTITY() AS BIGINT);
        ';

        DECLARE @params NVARCHAR(MAX) = N'
            @movie_id   BIGINT,
            @episode_id BIGINT,
            @user_id    BIGINT,
            @user_name  NVARCHAR(255),
            @avatar_url NVARCHAR(500),
            @content    NVARCHAR(MAX),
            @parent_id  BIGINT,
            @new_id     BIGINT OUTPUT
        ';

        DECLARE @new_id BIGINT = 0;
        EXEC sp_executesql @sql, @params,
            @movie_id   = @p_movie_id,
            @episode_id = @p_episode_id,
            @user_id    = @p_user_id,
            @user_name  = @p_user_name,
            @avatar_url = @p_avatar_url,
            @content    = @p_content,
            @parent_id  = @p_parent_id,
            @new_id     = @new_id OUTPUT;

        SET @o_comment_id = ISNULL(@new_id, 0);
        SET @o_code = N'200';
        SET @o_message = N'Thêm bình luận thành công';
    END TRY
    BEGIN CATCH
        SET @o_comment_id = 0;
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
