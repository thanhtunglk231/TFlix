SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Description:   Lấy danh sách phim lẻ mới nhất cho khu vực Phim lẻ mới nhất ở Trang chủ
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_movies_get_latest
    @p_limit   INT = 8,
    @o_code    VARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @p_limit IS NULL OR @p_limit <= 0 SET @p_limit = 8;

        SELECT TOP (@p_limit)
            m.movie_id       AS moviE_ID,
            N'MOVIE'         AS kind,
            m.title          AS title,
            m.original_title AS originaL_TITLE,
            m.release_date   AS releasE_DATE,
            m.status         AS status,
            ISNULL(m.is_premium, 'N') AS iS_PREMIUM,
            COALESCE(ma_p.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS posteR_URL,
            COALESCE(ma_b.url, ma_p.url, N'https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?q=80&w=1600&auto=format&fit=crop') AS backdroP_URL,
            (SELECT COUNT(1) FROM dbo.video_sources vs WHERE vs.movie_id = m.movie_id) AS sourcE_COUNT,
            (
                SELECT STRING_AGG(g.genre_name, N', ')
                FROM dbo.movie_genres mg
                JOIN dbo.genres g ON mg.genre_id = g.genre_id
                WHERE mg.movie_id = m.movie_id
            ) AS genres
        FROM dbo.movies m
        LEFT JOIN dbo.movie_assets ma_p ON m.movie_id = ma_p.movie_id AND ma_p.asset_type = N'POSTER'
        LEFT JOIN dbo.movie_assets ma_b ON m.movie_id = ma_b.movie_id AND ma_b.asset_type = N'BACKDROP'
        WHERE (m.status IS NULL OR m.status = N'PUBLISHED')
        ORDER BY m.release_date DESC, m.movie_id DESC;

        SET @o_code = '200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
