SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[sp_episode_add]
    @p_series_id     INT,
    @p_season_id     INT,
    @p_episode_no    INT,
    @p_title         NVARCHAR(255),
    @p_air_date      DATE = NULL,
    @p_duration_min  INT = NULL,
    @p_overview      NVARCHAR(MAX) = NULL,
    @p_status        NVARCHAR(50) = 'PUBLISHED',
    @p_is_premium    CHAR(1) = 'N',

    @o_episode_id    INT OUTPUT,
    @o_code          NVARCHAR(10) OUTPUT,
    @o_message       NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @v_cnt INT;

    BEGIN TRY
        -- Check season thuộc series
        SELECT @v_cnt = COUNT(*)
        FROM seasons
        WHERE season_id = @p_season_id
          AND series_id = @p_series_id;

        IF @v_cnt = 0
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Season không thuộc Series';
            RETURN;
        END

        -- Insert
        INSERT INTO episodes (
            series_id, season_id, episode_no, title, overview,
            air_date, duration_min, status, is_premium
        )
        VALUES (
            @p_series_id, @p_season_id, @p_episode_no, @p_title, @p_overview,
            @p_air_date, @p_duration_min, @p_status, @p_is_premium
        );

        -- Lấy ID vừa insert
        SET @o_episode_id = SCOPE_IDENTITY();

        SET @o_code = '200';
        SET @o_message = N'OK';
    END TRY

    BEGIN CATCH
        DECLARE @err INT = ERROR_NUMBER();
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();

        IF @err = 2627 OR @err = 2601
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'episode_no đã tồn tại trong season';
        END
        ELSE IF @err = 547
        BEGIN
            SET @o_code = '400';
            SET @o_message = N'Mã tham chiếu không hợp lệ (series/season)';
        END
        ELSE
        BEGIN
            SET @o_code = '500';
            SET @o_message = N'Lỗi: ' + @msg;
        END
    END CATCH
END;
GO
