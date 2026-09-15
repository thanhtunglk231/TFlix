SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- Author:        TFlix Team
-- Create date:   2026-09-13
-- Description:   Script & Stored Procedure thêm 10 bộ phim mẫu đa dạng thể loại,
--                quốc gia, năm phát hành để kiểm thử bộ lọc trang Kho phim.
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_seed_10_sample_movies
    @o_code    NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- 1. Ensure sample Genres exist
        IF NOT EXISTS (SELECT 1 FROM dbo.genres WHERE genre_name = N'Hành động')
            INSERT INTO dbo.genres(genre_name) VALUES (N'Hành động');
        IF NOT EXISTS (SELECT 1 FROM dbo.genres WHERE genre_name = N'Tình cảm')
            INSERT INTO dbo.genres(genre_name) VALUES (N'Tình cảm');
        IF NOT EXISTS (SELECT 1 FROM dbo.genres WHERE genre_name = N'Viễn tưởng')
            INSERT INTO dbo.genres(genre_name) VALUES (N'Viễn tưởng');
        IF NOT EXISTS (SELECT 1 FROM dbo.genres WHERE genre_name = N'Kinh dị')
            INSERT INTO dbo.genres(genre_name) VALUES (N'Kinh dị');
        IF NOT EXISTS (SELECT 1 FROM dbo.genres WHERE genre_name = N'Hoạt hình')
            INSERT INTO dbo.genres(genre_name) VALUES (N'Hoạt hình');

        DECLARE @g_hanhdong INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name = N'Hành động');
        DECLARE @g_tinhcam  INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name = N'Tình cảm');
        DECLARE @g_vientuong INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name = N'Viễn tưởng');
        DECLARE @g_kinhdi   INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name = N'Kinh dị');
        DECLARE @g_hoathinh INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name = N'Hoạt hình');

        -- Table structure for 10 movies
        DECLARE @MoviesTable TABLE (
            id INT IDENTITY(1,1),
            title NVARCHAR(500),
            original_title NVARCHAR(500),
            overview NVARCHAR(MAX),
            release_date DATETIME,
            duration_min INT,
            country_code NVARCHAR(10),
            language_code NVARCHAR(10),
            is_premium CHAR(1),
            poster_url NVARCHAR(1000),
            genre_id INT
        );

        INSERT INTO @MoviesTable (title, original_title, overview, release_date, duration_min, country_code, language_code, is_premium, poster_url, genre_id)
        VALUES
        (N'Lật Mặt 7: Một Điều Ước', N'Wish 7', N'Câu chuyện cảm động về tình thân gia đình bà Hai và 5 người con.', '2024-04-26', 138, N'VN', N'vi', 'Y', N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop', @g_hanhdong),
        (N'Mai', N'Mai Movie', N'Chuyện tình đầy trăn trở giữa Mai và Dương tại khu chung cư cũ.', '2024-02-10', 131, N'VN', N'vi', 'N', N'https://images.unsplash.com/photo-1485846234645-a62644f84728?w=500&auto=format&fit=crop', @g_tinhcam),
        (N'Avatar: Dòng Chảy Của Nước', N'Avatar: The Way of Water', N'Jake Sully và Neytiri bảo vệ gia đình trước mối đe dọa mới tại hành tinh Pandora.', '2022-12-16', 192, N'US', N'en', 'Y', N'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop', @g_vientuong),
        (N'Oppenheimer', N'Oppenheimer', N'Cuộc đời và quá trình sáng tạo ra bom nguyên tử của nhà vật lý J. Robert Oppenheimer.', '2023-07-21', 180, N'US', N'en', 'Y', N'https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=500&auto=format&fit=crop', @g_hanhdong),
        (N'Quật Mộ Trùng Phùng', N'Exhuma', N'Hai pháp sư và thầy phong thủy khai quật ngôi mộ cổ bí ẩn tại Hàn Quốc.', '2024-02-22', 134, N'KR', N'kr', 'Y', N'https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500&auto=format&fit=crop', @g_kinhdi),
        (N'Ký Sinh Trùng', N'Parasite', N'Sự tương phản gay gắt giữa hai gia đình giàu nghèo tại Seoul.', '2019-05-30', 132, N'KR', N'kr', 'N', N'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=500&auto=format&fit=crop', @g_tinhcam),
        (N'Tây Du Ký: Mối Tình Ngoại Truyện', N'Journey to the West', N'Hành trình trừ yêu diệt quỷ hài hước và ly kỳ của Đường Tăng.', '2021-02-12', 110, N'CN', N'zh', 'N', N'https://images.unsplash.com/photo-1533929736458-ca588d08c8be?w=500&auto=format&fit=crop', @g_hanhdong),
        (N'Suzume Cửa Hàng Khóa Ký Ức', N'Suzume no Tojimari', N'Hành trình đi khắp Nhật Bản để đóng những cánh cửa thảm họa của cô gái Suzume.', '2023-03-08', 122, N'JP', N'ja', 'N', N'https://images.unsplash.com/photo-1578632767115-351597cf2477?w=500&auto=format&fit=crop', @g_hoathinh),
        (N'Godzilla x Kong: Đế Chế Mới', N'Godzilla x Kong: The New Empire', N'Godzilla và Kong bắt tay chống lại hiểm họa quái thú khổng lồ giấu mặt.', '2024-03-29', 115, N'US', N'en', 'Y', N'https://images.unsplash.com/photo-1563089145-599997674d42?w=500&auto=format&fit=crop', @g_vientuong),
        (N'Em Và Trịnh', N'Em and Trinh', N'Tái hiện cuộc đời tài hoa và những mối tình khắc ghi của nhạc sĩ Trịnh Công Sơn.', '2022-06-17', 136, N'VN', N'vi', 'N', N'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=500&auto=format&fit=crop', @g_tinhcam);

        -- Loop and Insert
        DECLARE @idx INT = 1;
        DECLARE @count INT = (SELECT COUNT(1) FROM @MoviesTable);
        DECLARE @new_movie_id DECIMAL;

        WHILE @idx <= @count
        BEGIN
            DECLARE @title NVARCHAR(500), @orig NVARCHAR(500), @over NVARCHAR(MAX);
            DECLARE @rdate DATETIME, @dur INT, @country NVARCHAR(10), @lang NVARCHAR(10), @is_prem CHAR(1), @purl NVARCHAR(1000), @gid INT;

            SELECT 
                @title = title, @orig = original_title, @over = overview,
                @rdate = release_date, @dur = duration_min, @country = country_code,
                @lang = language_code, @is_prem = is_premium, @purl = poster_url, @gid = genre_id
            FROM @MoviesTable WHERE id = @idx;

            IF NOT EXISTS (SELECT 1 FROM dbo.movies WHERE title = @title)
            BEGIN
                INSERT INTO dbo.movies (title, original_title, overview, release_date, duration_min, country_code, language_code, status, is_premium)
                VALUES (@title, @orig, @over, @rdate, @dur, @country, @lang, N'PUBLISHED', @is_prem);

                SET @new_movie_id = SCOPE_IDENTITY();

                -- Insert Asset
                IF @new_movie_id IS NOT NULL AND @new_movie_id > 0
                BEGIN
                    INSERT INTO dbo.movie_assets (movie_id, asset_type, url)
                    VALUES (@new_movie_id, N'POSTER', @purl);

                    IF @gid IS NOT NULL AND @gid > 0
                    BEGIN
                        INSERT INTO dbo.movie_genres (movie_id, genre_id)
                        VALUES (@new_movie_id, @gid);
                    END;
                END;
            END;

            SET @idx = @idx + 1;
        END;

        SET @o_code = N'200';
        SET @o_message = N'Thêm 10 phim mẫu thành công!';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
