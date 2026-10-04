SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Description:   Lấy toàn bộ danh sách phim đang hiển thị cho trang chủ / danh mục
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_get_all_movie
    @o_code    NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            m.movie_id       AS moviE_ID,
            m.title          AS title,
            m.original_title AS originaL_TITLE,
            m.overview       AS overview,
            m.release_date   AS releasE_DATE,
            m.duration_min   AS duratioN_MIN,
            m.country_code   AS countrY_CODE,
            m.language_code  AS languagE_CODE,
            m.status         AS status,
            ISNULL(m.is_premium, 'N') AS iS_PREMIUM,
            m.created_at     AS createD_AT,
            COALESCE(ma.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS posteR_URL,
            (
                SELECT STRING_AGG(g.genre_name, N', ')
                FROM dbo.movie_genres mg
                JOIN dbo.genres g ON mg.genre_id = g.genre_id
                WHERE mg.movie_id = m.movie_id
            ) AS genres,
            ISNULL((SELECT AVG(CAST(r.rating_val AS DECIMAL(3,1))) FROM dbo.ratings r WHERE r.movie_id = m.movie_id), 8.5) AS avG_RATING,
            (SELECT COUNT(1) FROM dbo.video_sources vs WHERE vs.movie_id = m.movie_id) AS sourcE_COUNT
        FROM dbo.movies m
        LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N'POSTER'
        WHERE (m.status IS NULL OR m.status = N'PUBLISHED')
        ORDER BY m.release_date DESC, m.movie_id DESC;

        SET @o_code = N'200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
