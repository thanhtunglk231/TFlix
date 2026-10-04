SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Description:   Lấy danh sách bình luận theo Movie hoặc Episode (tự động JOIN app_users nếu có)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Comment_GetByContent
    @p_movie_id   BIGINT = NULL,
    @p_episode_id BIGINT = NULL,
    @o_code       NVARCHAR(10) OUTPUT,
    @o_message    NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
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

        DECLARE @has_username BIT = CASE WHEN COL_LENGTH('dbo.comments', 'user_name') IS NOT NULL THEN 1 ELSE 0 END;
        DECLARE @has_avatar   BIT = CASE WHEN COL_LENGTH('dbo.comments', 'avatar_url') IS NOT NULL THEN 1 ELSE 0 END;
        DECLARE @has_appusers BIT = CASE WHEN OBJECT_ID('dbo.app_users', 'U') IS NOT NULL THEN 1 ELSE 0 END;

        -- Xác định cột content
        DECLARE @content_col NVARCHAR(100) = N'content';
        IF COL_LENGTH(N'dbo.comments', N'content') IS NOT NULL
            SET @content_col = N'c.content';
        ELSE IF COL_LENGTH(N'dbo.comments', N'comment_text') IS NOT NULL
            SET @content_col = N'c.comment_text';
        ELSE IF COL_LENGTH(N'dbo.comments', N'body') IS NOT NULL
            SET @content_col = N'c.body';
        ELSE
            SET @content_col = N'c.content';

        DECLARE @user_select NVARCHAR(MAX);
        IF @has_appusers = 1 AND @has_username = 1
            SET @user_select = N'COALESCE(u.full_name, c.user_name, N''Người dùng TFlix'') AS UserName, COALESCE(u.avatar_url, c.avatar_url) AS UserAvatar,';
        ELSE IF @has_appusers = 1
            SET @user_select = N'COALESCE(u.full_name, N''Người dùng TFlix'') AS UserName, u.avatar_url AS UserAvatar,';
        ELSE IF @has_username = 1
            SET @user_select = N'COALESCE(c.user_name, N''Người dùng TFlix'') AS UserName, ' + CASE WHEN @has_avatar = 1 THEN N'c.avatar_url' ELSE N'NULL' END + N' AS UserAvatar,';
        ELSE
            SET @user_select = N'N''Người dùng TFlix'' AS UserName, NULL AS UserAvatar,';

        DECLARE @sql NVARCHAR(MAX) = N'
            SELECT 
                c.comment_id AS CommentId,
                c.movie_id   AS MovieId,
                c.episode_id AS EpisodeId,
                c.user_id    AS UserId,
                ' + @user_select + N'
                ' + @content_col + N' AS Content,
                c.created_at AS CreatedAt,
                ISNULL(c.status, ''ACTIVE'') AS Status
            FROM dbo.comments c
            ' + CASE WHEN @has_appusers = 1 THEN N'LEFT JOIN dbo.app_users u ON c.user_id = u.user_id' ELSE N'' END + N'
            WHERE (c.status IS NULL OR c.status = ''ACTIVE'')
              AND (
                    (@episode_id IS NOT NULL AND c.episode_id = @episode_id)
                    OR
                    (@episode_id IS NULL AND @movie_id IS NOT NULL AND c.movie_id = @movie_id AND (c.episode_id IS NULL OR c.episode_id = 0))
                  )
            ORDER BY c.created_at DESC;
        ';

        EXEC sp_executesql @sql,
            N'@movie_id BIGINT, @episode_id BIGINT',
            @movie_id = @p_movie_id,
            @episode_id = @p_episode_id;

        SET @o_code = N'200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
