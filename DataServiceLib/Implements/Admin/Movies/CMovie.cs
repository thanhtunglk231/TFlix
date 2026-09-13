using CoreLib.Dtos.AuthDtos;
using CoreLib.Dtos.Movies;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovie : ICMovie
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovie(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }


        public async Task<CResponseMessage> get_all()
        {
            try
            {


                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_get_all_movie", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "400",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }
        public async Task<CResponseMessage> Add_movie(AddMovieDto addMovieDto)
        {
            try
            {
                // IN parameters
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Title ?? DBNull.Value };
                var p_release_date = new SqlParameter("@p_release_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.ReleaseDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.DurationMin ?? DBNull.Value };
                // Trim codes to typical 2-char ISO codes to avoid DB truncation if DB column is short
                string? lang = addMovieDto.LanguageCode;
                if (!string.IsNullOrWhiteSpace(lang) && lang.Length > 2) lang = lang.Substring(0, 2);
                string? country = addMovieDto.CountryCode;
                if (!string.IsNullOrWhiteSpace(country) && country.Length > 2) country = country.Substring(0, 2);
                var p_language_code = new SqlParameter("@p_language_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)lang ?? DBNull.Value };

                var p_original_title = new SqlParameter("@p_original_title", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.OriginalTitle ?? DBNull.Value };
                var p_country_code = new SqlParameter("@p_country_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)country ?? DBNull.Value };
                // ✅ CHAR(1) cho Y/N
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = addMovieDto.IsPremium ? "Y" : "N" };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)addMovieDto.Status ?? "PUBLISHED" };

                // OUT parameters
                var o_movie_id = new SqlParameter("@o_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
            p_title,p_original_title, p_release_date, p_duration_min,
            p_language_code, p_country_code, p_is_premium,
            p_overview, p_status,
            o_movie_id, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_add", parameters, _connectionString);

                // ✅ ĐỌC OUT NUMBER ĐÚNG CÁCH
                decimal? newId = null;
                if (o_movie_id.Value != null && o_movie_id.Value != DBNull.Value)
                {
                    newId = Convert.ToDecimal(o_movie_id.Value);
                }

                return new CResponseMessage
                {
                    Data = new { DataSet = dataset, MovieId = newId },
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> Update_movie(UpdateMovieDto updateMovieDto)
        {
            try
            {
                // IN parameters
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = updateMovieDto.MovieId };
                var p_title = new SqlParameter("@p_title", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Title ?? DBNull.Value };
                var p_original_title = new SqlParameter("@p_original_title", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.OriginalTitle ?? DBNull.Value };

                var p_release_date = new SqlParameter("@p_release_date", SqlDbType.DateTime) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.ReleaseDate ?? DBNull.Value };
                var p_duration_min = new SqlParameter("@p_duration_min", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.DurationMin ?? DBNull.Value };
                string? ulang = updateMovieDto.LanguageCode;
                if (!string.IsNullOrWhiteSpace(ulang) && ulang.Length > 2) ulang = ulang.Substring(0, 2);
                string? ucountry = updateMovieDto.CountryCode;
                if (!string.IsNullOrWhiteSpace(ucountry) && ucountry.Length > 2) ucountry = ucountry.Substring(0, 2);

                var p_language_code = new SqlParameter("@p_language_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)ulang ?? DBNull.Value };
                var p_country_code = new SqlParameter("@p_country_code", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)ucountry ?? DBNull.Value };
                var p_is_premium = new SqlParameter("@p_is_premium", SqlDbType.Char, 1) { Direction = ParameterDirection.Input, Value = updateMovieDto.IsPremium ? "Y" : "N" };
                var p_overview = new SqlParameter("@p_overview", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Overview ?? DBNull.Value };
                var p_status = new SqlParameter("@p_status", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)updateMovieDto.Status ?? "PUBLISHED" };

                // OUT parameters
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
            p_movie_id, p_title,p_original_title, p_release_date, p_duration_min,
            p_language_code, p_country_code, p_is_premium,
            p_overview, p_status,
            o_code, o_message
                };

                // Gọi stored procedure
                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_update", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // có thể null vì SP này không mở cursor
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }


        public async Task<CResponseMessage> Delete_movie(decimal movieId)
        {
            try
            {
                // IN parameter
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Decimal) { Direction = ParameterDirection.Input, Value = movieId };

                // OUT parameters
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, o_code, o_message };

                // Gọi stored procedure
                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_delete", parameters, _connectionString);

                return new CResponseMessage
                {
                    Data = dataset, // SP này không có cursor nên dataset có thể rỗng
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        private async Task EnsureStoredProcedureExistsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = @"
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_movie_catalog_search]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE dbo.sp_movie_catalog_search
                            @p_search         NVARCHAR(500) = NULL,
                            @p_genre_id       INT = NULL,
                            @p_country_code   NVARCHAR(10) = NULL,
                            @p_year           INT = NULL,
                            @p_sort_by        NVARCHAR(50) = N''newest'',
                            @p_page           INT = 1,
                            @p_page_size      INT = 12,
                            @o_total_count    INT OUTPUT,
                            @o_code           NVARCHAR(10) OUTPUT,
                            @o_message        NVARCHAR(4000) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            BEGIN TRY
                                IF @p_page IS NULL OR @p_page < 1 SET @p_page = 1;
                                IF @p_page_size IS NULL OR @p_page_size < 1 SET @p_page_size = 12;
                                DECLARE @Offset INT = (@p_page - 1) * @p_page_size;
                                SET @p_search = NULLIF(TRIM(@p_search), N'''');
                                SET @p_country_code = NULLIF(TRIM(@p_country_code), N'''');

                                SELECT @o_total_count = COUNT(DISTINCT m.movie_id)
                                FROM dbo.movies m
                                LEFT JOIN dbo.movie_genres mg ON m.movie_id = mg.movie_id
                                WHERE (m.status IS NULL OR m.status = N''PUBLISHED'')
                                  AND (@p_search IS NULL OR m.title LIKE N''%'' + @p_search + N''%'' OR m.original_title LIKE N''%'' + @p_search + N''%'')
                                  AND (@p_genre_id IS NULL OR mg.genre_id = @p_genre_id)
                                  AND (@p_country_code IS NULL OR m.country_code = @p_country_code)
                                  AND (@p_year IS NULL OR YEAR(m.release_date) = @p_year);

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
                                    COALESCE(ma.url, N''https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop'') AS PosterUrl,
                                    (
                                        SELECT STRING_AGG(g.genre_name, N'', '')
                                        FROM dbo.movie_genres mg2
                                        JOIN dbo.genres g ON mg2.genre_id = g.genre_id
                                        WHERE mg2.movie_id = m.movie_id
                                    ) AS Genres
                                FROM dbo.movies m
                                LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N''POSTER''
                                WHERE m.movie_id IN (
                                    SELECT DISTINCT m2.movie_id
                                    FROM dbo.movies m2
                                    LEFT JOIN dbo.movie_genres mg ON m2.movie_id = mg.movie_id
                                    WHERE (m2.status IS NULL OR m2.status = N''PUBLISHED'')
                                      AND (@p_search IS NULL OR m2.title LIKE N''%'' + @p_search + N''%'' OR m2.original_title LIKE N''%'' + @p_search + N''%'')
                                      AND (@p_genre_id IS NULL OR mg.genre_id = @p_genre_id)
                                      AND (@p_country_code IS NULL OR m2.country_code = @p_country_code)
                                      AND (@p_year IS NULL OR YEAR(m2.release_date) = @p_year)
                                )
                                ORDER BY 
                                    CASE WHEN @p_sort_by = N''oldest'' THEN m.release_date END ASC,
                                    CASE WHEN @p_sort_by = N''title'' THEN m.title END ASC,
                                    m.release_date DESC,
                                    m.movie_id DESC
                                OFFSET @Offset ROWS
                                FETCH NEXT @p_page_size ROWS ONLY;

                                SET @o_code = N''200'';
                                SET @o_message = N''Thành công'';
                            END TRY
                            BEGIN CATCH
                                SET @o_total_count = 0;
                                SET @o_code = N''500'';
                                SET @o_message = ERROR_MESSAGE();
                            END CATCH
                        END;
                        ')
                    END;
                ";
                using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EnsureStoredProcedureExistsAsync] Error: " + ex.Message);
            }
        }

        public async Task<CResponseMessage> GetCatalogMovies(MovieCatalogFilterDto filter)
        {
            try
            {
                await EnsureStoredProcedureExistsAsync();

                var p_search = new SqlParameter("@p_search", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)filter.Search ?? DBNull.Value };
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)filter.GenreId ?? DBNull.Value };
                var p_country_code = new SqlParameter("@p_country_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Input, Value = (object?)filter.CountryCode ?? DBNull.Value };
                var p_year = new SqlParameter("@p_year", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)filter.Year ?? DBNull.Value };
                var p_sort_by = new SqlParameter("@p_sort_by", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = (object?)filter.SortBy ?? "newest" };
                var p_page = new SqlParameter("@p_page", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = filter.Page };
                var p_page_size = new SqlParameter("@p_page_size", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = filter.PageSize };

                var o_total_count = new SqlParameter("@o_total_count", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
                    p_search, p_genre_id, p_country_code, p_year, p_sort_by, p_page, p_page_size,
                    o_total_count, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_catalog_search", parameters, _connectionString);

                int totalCount = 0;
                if (o_total_count.Value != null && o_total_count.Value != DBNull.Value)
                {
                    totalCount = Convert.ToInt32(o_total_count.Value);
                }

                Func<object?, DateTime?> parseDate = (obj) =>
                {
                    if (obj == null || obj == DBNull.Value) return null;
                    if (obj is DateTimeOffset dto) return dto.DateTime;
                    if (obj is DateTime dt) return dt;
                    if (DateTime.TryParse(obj.ToString(), out var d)) return d;
                    return null;
                };

                var items = new List<MovieCatalogItemDto>();
                if (dataset != null && dataset.Tables.Count > 0)
                {
                    var dt = dataset.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        items.Add(new MovieCatalogItemDto
                        {
                            MovieId = Convert.ToInt64(row["MovieId"]),
                            Title = row["Title"]?.ToString() ?? "",
                            OriginalTitle = row["OriginalTitle"] == DBNull.Value ? null : row["OriginalTitle"]?.ToString(),
                            Overview = row["Overview"] == DBNull.Value ? null : row["Overview"]?.ToString(),
                            ReleaseDate = parseDate(row["ReleaseDate"]),
                            DurationMin = row["DurationMin"] == DBNull.Value ? null : Convert.ToInt32(row["DurationMin"]),
                            CountryCode = row["CountryCode"] == DBNull.Value ? null : row["CountryCode"]?.ToString(),
                            LanguageCode = row["LanguageCode"] == DBNull.Value ? null : row["LanguageCode"]?.ToString(),
                            Status = row["Status"] == DBNull.Value ? null : row["Status"]?.ToString(),
                            IsPremiumYN = row["IsPremiumYN"]?.ToString() ?? "N",
                            CreatedAt = parseDate(row["CreatedAt"]) ?? DateTime.Now,
                            PosterUrl = row["PosterUrl"] == DBNull.Value ? null : row["PosterUrl"]?.ToString(),
                            Genres = row["Genres"] == DBNull.Value ? null : row["Genres"]?.ToString()
                        });
                    }
                }

                var result = new MovieCatalogResultDto
                {
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Items = items
                };

                return new CResponseMessage
                {
                    Data = result,
                    code = o_code.Value?.ToString() ?? "200",
                    message = o_message.Value?.ToString() ?? "Thành công",
                    Success = (o_code.Value?.ToString() == "200")
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> SeedSampleMovies()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sqlScript = @"
                    DECLARE @g_hanhdong INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name LIKE N'%Hành động%' OR genre_name LIKE N'%Action%');
                    IF @g_hanhdong IS NULL SET @g_hanhdong = (SELECT TOP 1 genre_id FROM dbo.genres);

                    DECLARE @g_tinhcam  INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name LIKE N'%Tình cảm%' OR genre_name LIKE N'%Drama%');
                    IF @g_tinhcam IS NULL SET @g_tinhcam = (SELECT TOP 1 genre_id FROM dbo.genres);

                    DECLARE @g_vientuong INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name LIKE N'%Viễn tưởng%' OR genre_name LIKE N'%Sci-Fi%');
                    IF @g_vientuong IS NULL SET @g_vientuong = (SELECT TOP 1 genre_id FROM dbo.genres);

                    DECLARE @g_kinhdi   INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name LIKE N'%Kinh dị%' OR genre_name LIKE N'%Horror%');
                    IF @g_kinhdi IS NULL SET @g_kinhdi = (SELECT TOP 1 genre_id FROM dbo.genres);

                    DECLARE @g_hoathinh INT = (SELECT TOP 1 genre_id FROM dbo.genres WHERE genre_name LIKE N'%Hoạt hình%' OR genre_name LIKE N'%Animation%');
                    IF @g_hoathinh IS NULL SET @g_hoathinh = (SELECT TOP 1 genre_id FROM dbo.genres);

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

                    DECLARE @idx INT = 1;
                    DECLARE @count INT = (SELECT COUNT(1) FROM @MoviesTable);
                    DECLARE @inserted_count INT = 0;

                    WHILE @idx <= @count
                    BEGIN
                        DECLARE @title NVARCHAR(500), @orig NVARCHAR(500), @over NVARCHAR(MAX);
                        DECLARE @rdate DATETIME, @dur INT, @country NVARCHAR(10), @lang NVARCHAR(10), @is_prem CHAR(1), @purl NVARCHAR(1000), @gid INT;
                        DECLARE @new_id DECIMAL = 0;

                        SELECT 
                            @title = title, @orig = original_title, @over = overview,
                            @rdate = release_date, @dur = duration_min, @country = country_code,
                            @lang = language_code, @is_prem = is_premium, @purl = poster_url, @gid = genre_id
                        FROM @MoviesTable WHERE id = @idx;

                        IF NOT EXISTS (SELECT 1 FROM dbo.movies WHERE title = @title)
                        BEGIN
                            INSERT INTO dbo.movies (title, original_title, overview, release_date, duration_min, country_code, language_code, status, is_premium)
                            VALUES (@title, @orig, @over, @rdate, @dur, @country, @lang, N'PUBLISHED', @is_prem);

                            SET @new_id = SCOPE_IDENTITY();

                            IF @new_id > 0
                            BEGIN
                                INSERT INTO dbo.movie_assets (movie_id, asset_type, url)
                                VALUES (@new_id, N'POSTER', @purl);

                                IF @gid IS NOT NULL AND @gid > 0
                                BEGIN
                                    INSERT INTO dbo.movie_genres (movie_id, genre_id)
                                    VALUES (@new_id, @gid);
                                END;

                                SET @inserted_count = @inserted_count + 1;
                            END;
                        END;

                        SET @idx = @idx + 1;
                    END;

                    SELECT @inserted_count;
                ";

                using var cmd = new SqlCommand(sqlScript, conn);
                var inserted = await cmd.ExecuteScalarAsync();

                return new CResponseMessage
                {
                    Success = true,
                    code = "200",
                    message = $"Thành công! Đã thêm mới {inserted} phim mẫu vào cơ sở dữ liệu."
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi seed dữ liệu: " + ex.Message
                };
            }
        }
    }
}
