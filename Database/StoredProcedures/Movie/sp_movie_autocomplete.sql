CREATE OR ALTER PROCEDURE dbo.sp_movie_autocomplete
    @p_query   NVARCHAR(200),
    @p_limit   INT = 8,
    @o_code    NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SET @p_query = LTRIM(RTRIM(ISNULL(@p_query, N'')));
        SET @p_limit = CASE WHEN ISNULL(@p_limit, 8) BETWEEN 1 AND 12 THEN @p_limit ELSE 8 END;

        SELECT TOP (@p_limit)
            m.movie_id AS MovieId,
            m.title AS Title,
            m.original_title AS OriginalTitle,
            m.release_date AS ReleaseDate,
                        (
                                SELECT STRING_AGG(p.full_name, N', ')
                                FROM movie_people mp
                                INNER JOIN people p ON p.person_id = mp.person_id
                                WHERE mp.movie_id = m.movie_id
                                    AND UPPER(mp.job) = N'PRODUCER'
                        ) AS ProducerName,
            (
                SELECT TOP 1 ma.url
                FROM movie_assets ma
                WHERE ma.movie_id = m.movie_id
                  AND ma.asset_type = 'POSTER'
                ORDER BY ISNULL(ma.sort_order, 999999), ma.asset_id
            ) AS PosterUrl,
            'MOVIE' AS Kind
        FROM movies m
        WHERE m.status = 'PUBLISHED'
          AND (
              m.title LIKE N'%' + @p_query + N'%'
              OR m.original_title LIKE N'%' + @p_query + N'%'
                            OR EXISTS (
                                    SELECT 1
                                    FROM movie_people mp
                                    INNER JOIN people p ON p.person_id = mp.person_id
                                    WHERE mp.movie_id = m.movie_id
                                        AND UPPER(mp.job) = N'PRODUCER'
                                        AND (p.full_name LIKE N'%' + @p_query + N'%' OR p.also_known LIKE N'%' + @p_query + N'%')
                            )
          )
        ORDER BY m.created_at DESC, m.movie_id DESC;

        SET @o_code = '200';
        SET @o_message = N'Tìm phim thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = '500';
        SET @o_message = N'Lỗi: ' + ERROR_MESSAGE();

        SELECT TOP 0
            CAST(NULL AS INT) AS MovieId,
            CAST(NULL AS NVARCHAR(255)) AS Title,
            CAST(NULL AS NVARCHAR(255)) AS OriginalTitle,
            CAST(NULL AS DATE) AS ReleaseDate,
            CAST(NULL AS NVARCHAR(MAX)) AS ProducerName,
            CAST(NULL AS NVARCHAR(1000)) AS PosterUrl,
            CAST(NULL AS NVARCHAR(10)) AS Kind;
    END CATCH
END;
