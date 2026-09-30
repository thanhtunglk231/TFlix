SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Create date:   2026-09-15
-- Description:   Lấy thông tin chi tiết của phim / series cho trang Preview Details
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.SP_GET_CONTENT_BY_ID
    @p_id BIGINT = NULL,
    @p_code NVARCHAR(50) = NULL,
    @p_kind NVARCHAR(50) = N'movie',
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @p_id IS NULL AND @p_code IS NOT NULL AND ISNUMERIC(@p_code) = 1
        BEGIN
            SET @p_id = CAST(@p_code AS BIGINT);
        END;

        SELECT TOP 1
            m.movie_id AS MovieId,
            m.movie_id AS id,
            m.title AS title,
            m.original_title AS OriginalTitle,
            m.overview AS OverviewText,
            m.release_date AS ReleaseOrAirDate,
            m.duration_min AS DurationMin,
            c.country_name AS CountryName,
            m.country_code AS CountryCode,
            l.language_name AS LanguageName,
            m.language_code AS LanguageCode,
            m.status AS Status,
            m.is_premium AS IsPremiumYN,
            COALESCE(ma.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS PrimaryPosterUrl,
            vs.stream_url AS PrimaryStreamUrl,
            (
                SELECT STRING_AGG(g.genre_name, N', ')
                FROM dbo.movie_genres mg
                JOIN dbo.genres g ON mg.genre_id = g.genre_id
                WHERE mg.movie_id = m.movie_id
            ) AS genres,
            (
                SELECT STRING_AGG(p.full_name, N', ')
                FROM dbo.movie_people mp
                JOIN dbo.people p ON mp.person_id = p.person_id
                WHERE mp.movie_id = m.movie_id
            ) AS Casts,
            ISNULL(
                (SELECT AVG(CAST(r.rating_val AS FLOAT)) FROM dbo.ratings r WHERE r.movie_id = m.movie_id),
                8.5
            ) AS Rating
        FROM dbo.movies m
        LEFT JOIN dbo.countries c ON m.country_code = c.country_code
        LEFT JOIN dbo.languages l ON m.language_code = l.language_code
        LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N'POSTER'
        LEFT JOIN dbo.video_sources vs ON m.movie_id = vs.movie_id
        WHERE (@p_id IS NOT NULL AND m.movie_id = @p_id)
           OR (@p_id IS NULL AND 1=1);

        SET @o_code = N'200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
