SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Create date:   2026-09-13
-- Description:   Lấy danh sách phim cho trang Kho phim (Catalog) theo bộ lọc:
--                Tìm kiếm, Thể loại, Quốc gia, Năm, Sắp xếp và Phân trang.
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_movie_catalog_search
    @p_search         NVARCHAR(500) = NULL,
    @p_genre_id       INT = NULL,
    @p_country_code   NVARCHAR(10) = NULL,
    @p_year           INT = NULL,
    @p_sort_by        NVARCHAR(50) = N'newest',
    @p_page           INT = 1,
    @p_page_size      INT = 12,
    @o_total_count    INT OUTPUT,
    @o_code           NVARCHAR(10) OUTPUT,
    @o_message        NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validate Pagination
        IF @p_page IS NULL OR @p_page < 1 SET @p_page = 1;
        IF @p_page_size IS NULL OR @p_page_size < 1 SET @p_page_size = 12;

        DECLARE @Offset INT = (@p_page - 1) * @p_page_size;

        -- Clean input parameters
        SET @p_search = NULLIF(TRIM(@p_search), N'');
        SET @p_country_code = NULLIF(TRIM(@p_country_code), N'');

        -- Temporary storage / CTE for matching movies
        WITH FilteredMovies AS (
            SELECT DISTINCT
                m.movie_id,
                m.title,
                m.original_title,
                m.overview,
                m.release_date,
                m.duration_min,
                m.country_code,
                m.language_code,
                m.status,
                m.is_premium,
                m.created_at,
                COALESCE(ma.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS poster_url
            FROM dbo.movies m
            LEFT JOIN dbo.movie_genres mg ON m.movie_id = mg.movie_id
            LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N'POSTER'
            WHERE (m.status IS NULL OR m.status = N'PUBLISHED')
              AND (@p_search IS NULL OR m.title LIKE N'%' + @p_search + N'%' OR m.original_title LIKE N'%' + @p_search + N'%')
              AND (@p_genre_id IS NULL OR mg.genre_id = @p_genre_id)
              AND (@p_country_code IS NULL OR m.country_code = @p_country_code)
              AND (@p_year IS NULL OR YEAR(m.release_date) = @p_year)
        )
        SELECT @o_total_count = COUNT(1) FROM FilteredMovies;

        -- Final Select with Order By & Offset Fetch
        SELECT 
            m.movie_id           AS MovieId,
            m.title              AS Title,
            m.original_title     AS OriginalTitle,
            m.overview           AS Overview,
            m.release_date       AS ReleaseDate,
            m.duration_min       AS DurationMin,
            m.country_code       AS CountryCode,
            m.language_code      AS LanguageCode,
            m.status             AS Status,
            m.is_premium         AS IsPremiumYN,
            m.created_at         AS CreatedAt,
            COALESCE(ma.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS PosterUrl,
            (
                SELECT STRING_AGG(g.genre_name, N', ')
                FROM dbo.movie_genres mg2
                JOIN dbo.genres g ON mg2.genre_id = g.genre_id
                WHERE mg2.movie_id = m.movie_id
            ) AS Genres
        FROM dbo.movies m
        LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N'POSTER'
        WHERE m.movie_id IN (
            SELECT DISTINCT m2.movie_id
            FROM dbo.movies m2
            LEFT JOIN dbo.movie_genres mg ON m2.movie_id = mg.movie_id
            WHERE (m2.status IS NULL OR m2.status = N'PUBLISHED')
              AND (@p_search IS NULL OR m2.title LIKE N'%' + @p_search + N'%' OR m2.original_title LIKE N'%' + @p_search + N'%')
              AND (@p_genre_id IS NULL OR mg.genre_id = @p_genre_id)
              AND (@p_country_code IS NULL OR m2.country_code = @p_country_code)
              AND (@p_year IS NULL OR YEAR(m2.release_date) = @p_year)
        )
        ORDER BY 
            CASE WHEN @p_sort_by = N'oldest' THEN m.release_date END ASC,
            CASE WHEN @p_sort_by = N'title' THEN m.title END ASC,
            m.release_date DESC,
            m.movie_id DESC
        OFFSET @Offset ROWS
        FETCH NEXT @p_page_size ROWS ONLY;

        SET @o_code = N'200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_total_count = 0;
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
