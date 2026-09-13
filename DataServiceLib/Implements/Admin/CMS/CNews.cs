using CoreLib.Dtos.CMS;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.CMS
{
    public class CNews : ICNews
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CNews(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        private async Task EnsureNewsProcedureExistsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sqlScript = @"
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[post_categories]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE dbo.post_categories (
                            category_id INT IDENTITY(1,1) PRIMARY KEY,
                            category_name NVARCHAR(200) NOT NULL,
                            slug NVARCHAR(200) NULL,
                            created_at DATETIME DEFAULT GETDATE()
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[posts]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE dbo.posts (
                            post_id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            title NVARCHAR(500) NOT NULL,
                            slug NVARCHAR(500) NULL,
                            summary NVARCHAR(MAX) NULL,
                            content_html NVARCHAR(MAX) NULL,
                            thumbnail_url NVARCHAR(1000) NULL,
                            category_id INT NULL,
                            views_count INT DEFAULT 0,
                            is_featured CHAR(1) DEFAULT 'N',
                            status NVARCHAR(50) DEFAULT 'PUBLISHED',
                            created_at DATETIME DEFAULT GETDATE()
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_news_catalog_search]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE dbo.sp_news_catalog_search
                            @p_search         NVARCHAR(500) = NULL,
                            @p_category_id    INT = NULL,
                            @p_page           INT = 1,
                            @p_page_size      INT = 9,
                            @o_total_count    INT OUTPUT,
                            @o_code           NVARCHAR(10) OUTPUT,
                            @o_message        NVARCHAR(4000) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            BEGIN TRY
                                IF @p_page IS NULL OR @p_page < 1 SET @p_page = 1;
                                IF @p_page_size IS NULL OR @p_page_size < 1 SET @p_page_size = 9;
                                DECLARE @Offset INT = (@p_page - 1) * @p_page_size;

                                SET @p_search = NULLIF(TRIM(@p_search), N'''');

                                SELECT @o_total_count = COUNT(1)
                                FROM dbo.posts p
                                WHERE (p.status IS NULL OR p.status = N''PUBLISHED'')
                                  AND (@p_search IS NULL OR p.title LIKE N''%'' + @p_search + N''%'' OR p.summary LIKE N''%'' + @p_search + N''%'')
                                  AND (@p_category_id IS NULL OR p.category_id = @p_category_id);

                                SELECT 
                                    p.post_id           AS PostId,
                                    p.title             AS Title,
                                    p.slug              AS Slug,
                                    p.summary           AS Summary,
                                    COALESCE(p.thumbnail_url, N''https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop'') AS ThumbnailUrl,
                                    p.category_id       AS CategoryId,
                                    COALESCE(c.category_name, N''Tin tức chung'') AS CategoryName,
                                    COALESCE(p.views_count, 0) AS ViewsCount,
                                    COALESCE(p.is_featured, N''N'') AS IsFeatured,
                                    p.created_at        AS CreatedAt
                                FROM dbo.posts p
                                LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
                                WHERE (p.status IS NULL OR p.status = N''PUBLISHED'')
                                  AND (@p_search IS NULL OR p.title LIKE N''%'' + @p_search + N''%'' OR p.summary LIKE N''%'' + @p_search + N''%'')
                                  AND (@p_category_id IS NULL OR p.category_id = @p_category_id)
                                ORDER BY p.is_featured DESC, p.created_at DESC
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

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_news_get_by_id]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE dbo.sp_news_get_by_id
                            @p_post_id      BIGINT,
                            @o_code         NVARCHAR(10) OUTPUT,
                            @o_message      NVARCHAR(4000) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            BEGIN TRY
                                UPDATE dbo.posts
                                SET views_count = ISNULL(views_count, 0) + 1
                                WHERE post_id = @p_post_id;

                                SELECT 
                                    p.post_id           AS PostId,
                                    p.title             AS Title,
                                    p.slug              AS Slug,
                                    p.summary           AS Summary,
                                    p.content_html      AS ContentHtml,
                                    COALESCE(p.thumbnail_url, N''https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop'') AS ThumbnailUrl,
                                    p.category_id       AS CategoryId,
                                    COALESCE(c.category_name, N''Tin tức chung'') AS CategoryName,
                                    COALESCE(p.views_count, 1) AS ViewsCount,
                                    COALESCE(p.is_featured, N''N'') AS IsFeatured,
                                    p.created_at        AS CreatedAt
                                FROM dbo.posts p
                                LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
                                WHERE p.post_id = @p_post_id;

                                SELECT TOP 4
                                    p.post_id           AS PostId,
                                    p.title             AS Title,
                                    p.summary           AS Summary,
                                    COALESCE(p.thumbnail_url, N''https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop'') AS ThumbnailUrl,
                                    COALESCE(c.category_name, N''Tin tức'') AS CategoryName,
                                    p.created_at        AS CreatedAt
                                FROM dbo.posts p
                                LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
                                WHERE p.post_id <> @p_post_id
                                  AND (p.status IS NULL OR p.status = N''PUBLISHED'')
                                ORDER BY p.created_at DESC;

                                SET @o_code = N''200'';
                                SET @o_message = N''Thành công'';
                            END TRY
                            BEGIN CATCH
                                SET @o_code = N''500'';
                                SET @o_message = ERROR_MESSAGE();
                            END CATCH
                        END;
                        ')
                    END;
                ";

                using var cmd = new SqlCommand(sqlScript, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EnsureNewsProcedureExistsAsync] Error: " + ex.Message);
            }
        }

        public async Task<CResponseMessage> SeedSampleNews()
        {
            try
            {
                await EnsureNewsProcedureExistsAsync();
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string seedSql = @"
                    IF NOT EXISTS (SELECT 1 FROM dbo.post_categories)
                    BEGIN
                        INSERT INTO dbo.post_categories (category_name, slug) VALUES 
                        (N'Tin Điện Ảnh', N'tin-dien-anh'),
                        (N'Phim Chiếu Rạp', N'phim-chieu-rap'),
                        (N'Anime & Manga', N'anime-manga'),
                        (N'Sự Kiện & Khuyến Mãi', N'khuyen-mai');
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.posts)
                    BEGIN
                        DECLARE @c_dienanh INT = (SELECT TOP 1 category_id FROM dbo.post_categories WHERE slug = N'tin-dien-anh');
                        DECLARE @c_chieurap INT = (SELECT TOP 1 category_id FROM dbo.post_categories WHERE slug = N'phim-chieu-rap');
                        DECLARE @c_anime INT = (SELECT TOP 1 category_id FROM dbo.post_categories WHERE slug = N'anime-manga');
                        DECLARE @c_km INT = (SELECT TOP 1 category_id FROM dbo.post_categories WHERE slug = N'khuyen-mai');

                        INSERT INTO dbo.posts (title, slug, summary, content_html, thumbnail_url, category_id, views_count, is_featured, status, created_at)
                        VALUES
                        (
                            N'Top 10 Phim Bom Tấn Đáng Xem Nhất Mùa Hè Này Trên TFlix',
                            N'top-10-phim-bom-tan-dang-xem-nhat-mua-he',
                            N'Tổng hợp những siêu phẩm điện ảnh đỉnh cao vừa đổ bộ nền tảng TFlix với chất lượng 4K Ultra HD đỉnh cao.',
                            N'<p>Mùa hè này, nền tảng OTT <strong>TFlix</strong> mang đến cho khán giả một đại tiệc điện ảnh không thể bỏ lỡ với hàng loạt bom tấn chiếu rạp cực hot.</p><h2>1. Lật Mặt 7: Một Điều Ước</h2><p>Bộ phim gia đình đầy lấy đi nước mắt của triệu khán giả Việt đã chính thức có mặt trên TFlix. Hành trình tìm kiếm tình thân đầy xúc động của bà Hai và các con.</p><h2>2. Avatar: Dòng Chảy Của Nước</h2><p>Trải nghiệm thế giới Pandora kỹ xảo đỉnh cao của đạo diễn James Cameron với chất lượng hình ảnh sắc nét đến từng chi tiết.</p><p>Đăng ký gói VIP TFlix ngay hôm nay để không bỏ lỡ trải nghiệm xem phim không quảng cáo!</p>',
                            N'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=800&auto=format&fit=crop',
                            @c_dienanh, 1250, 'Y', N'PUBLISHED', GETDATE()
                        ),
                        (
                            N'Ưu Đãi Đặc Biệt: Đăng Ký Gói VIP TFlix Giảm 50% Khi Thanh Toán Qua MoMo',
                            N'uu-dai-dac-biet-dang-ky-goi-vip-tflix-giam-50-percent',
                            N'Chương trình khuyến mãi lớn nhất năm dành cho tất cả thành viên khi nâng cấp gói xem phim chất lượng cao.',
                            N'<p>Từ ngày 15/09 đến 30/09, TFlix phối hợp cùng ví điện tử MoMo mang đến chương trình ưu đãi độc quyền <strong>Giảm ngay 50%</strong> cho các gói VIP 3 tháng, 6 tháng và 12 tháng.</p><h3>Thể lệ chương trình:</h3><ul><li>Áp dụng cho khách hàng nâng cấp gói VIP thành công trên ứng dụng hoặc website TFlix.</li><li>Nhập mã <strong>TFLIX50</strong> tại bước thanh toán qua MoMo.</li></ul>',
                            N'https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=800&auto=format&fit=crop',
                            @c_km, 890, 'Y', N'PUBLISHED', DATEADD(DAY, -1, GETDATE())
                        ),
                        (
                            N'Suzume Cửa Hàng Khóa Ký Ức Đạt Cột Mốc Doanh Thu Kỷ Lục Tại Châu Á',
                            N'suzume-door-locking-record-box-office',
                            N'Tác phẩm mới nhất của phù thủy hoạt hình Makoto Shinkai tiếp tục chinh phục trái tim người hâm mộ anime toàn cầu.',
                            N'<p>Bộ phim hoạt hình <em>Suzume no Tojimari</em> đã đạt mức doanh thu kỷ lục tại các thị trường Nhật Bản, Hàn Quốc và Việt Nam. Câu chuyện xoay quanh cô gái trẻ Suzume và hành trình đóng lại những cánh cửa thảm họa.</p><p>Khán giả có thể thưởng thức tác phẩm này trực tiếp trên TFlix với bản quyền chính thức và phụ đề chuẩn.</p>',
                            N'https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&auto=format&fit=crop',
                            @c_anime, 640, 'N', N'PUBLISHED', DATEADD(DAY, -2, GETDATE())
                        ),
                        (
                            N'Hé Lộ Hậu Trường Kỹ Xảo Điện Ảnh Trong Bom Tấn Godzilla x Kong: Đế Chế Mới',
                            N'he-lo-hau-truong-ky-xao-godzilla-x-kong',
                            N'Khám phá công nghệ CGI hiện đại tạo nên những pha đại chiến nghẹt thở giữa các Titan khổng lồ.',
                            N'<p>Đội ngũ làm phim Hollywood đã mất hơn 2 năm để thiết kế tạo hình và mô phỏng chuyển động của Godzilla và Kong trong môi trường Trái Đất Rỗng.</p><p>Cùng xem lại hành trình hợp sức của hai đại quái thú trên TFlix với âm thanh vòm Dolby Atmos ấn tượng.</p>',
                            N'https://images.unsplash.com/photo-1563089145-599997674d42?w=800&auto=format&fit=crop',
                            @c_chieurap, 510, 'N', N'PUBLISHED', DATEADD(DAY, -3, GETDATE())
                        );
                    END;
                ";

                using var cmd = new SqlCommand(seedSql, conn);
                await cmd.ExecuteNonQueryAsync();

                return new CResponseMessage
                {
                    Success = true,
                    code = "200",
                    message = "Khởi tạo dữ liệu tin tức mẫu thành công!"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi seed tin tức: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> GetCatalogNews(NewsCatalogFilterDto filter)
        {
            try
            {
                await EnsureNewsProcedureExistsAsync();
                await SeedSampleNews();

                var p_search = new SqlParameter("@p_search", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = (object?)filter.Search ?? DBNull.Value };
                var p_category_id = new SqlParameter("@p_category_id", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = (object?)filter.CategoryId ?? DBNull.Value };
                var p_page = new SqlParameter("@p_page", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = filter.Page };
                var p_page_size = new SqlParameter("@p_page_size", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = filter.PageSize };

                var o_total_count = new SqlParameter("@o_total_count", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[]
                {
                    p_search, p_category_id, p_page, p_page_size,
                    o_total_count, o_code, o_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("sp_news_catalog_search", parameters, _connectionString);

                int totalCount = 0;
                if (o_total_count.Value != null && o_total_count.Value != DBNull.Value)
                {
                    totalCount = Convert.ToInt32(o_total_count.Value);
                }

                Func<object?, DateTime> parseDate = (obj) =>
                {
                    if (obj == null || obj == DBNull.Value) return DateTime.Now;
                    if (obj is DateTime dt) return dt;
                    if (DateTime.TryParse(obj.ToString(), out var d)) return d;
                    return DateTime.Now;
                };

                var items = new List<NewsItemDto>();
                if (dataset != null && dataset.Tables.Count > 0)
                {
                    var dt = dataset.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        items.Add(new NewsItemDto
                        {
                            PostId = Convert.ToInt64(row["PostId"]),
                            Title = row["Title"]?.ToString() ?? "",
                            Slug = row["Slug"] == DBNull.Value ? null : row["Slug"]?.ToString(),
                            Summary = row["Summary"] == DBNull.Value ? null : row["Summary"]?.ToString(),
                            ThumbnailUrl = row["ThumbnailUrl"] == DBNull.Value ? null : row["ThumbnailUrl"]?.ToString(),
                            CategoryId = row["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["CategoryId"]),
                            CategoryName = row["CategoryName"]?.ToString() ?? "Tin tức chung",
                            ViewsCount = row["ViewsCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["ViewsCount"]),
                            IsFeaturedYN = row["IsFeatured"]?.ToString() ?? "N",
                            CreatedAt = parseDate(row["CreatedAt"])
                        });
                    }
                }

                var result = new NewsCatalogResultDto
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
                    message = "Lỗi lấy danh sách tin tức: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> GetNewsDetail(long postId)
        {
            try
            {
                await EnsureNewsProcedureExistsAsync();
                await SeedSampleNews();

                var p_post_id = new SqlParameter("@p_post_id", SqlDbType.BigInt) { Direction = ParameterDirection.Input, Value = postId };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_post_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_news_get_by_id", parameters, _connectionString);

                Func<object?, DateTime> parseDate = (obj) =>
                {
                    if (obj == null || obj == DBNull.Value) return DateTime.Now;
                    if (obj is DateTime dt) return dt;
                    if (DateTime.TryParse(obj.ToString(), out var d)) return d;
                    return DateTime.Now;
                };

                NewsDetailDto? detail = null;
                var relatedList = new List<NewsItemDto>();

                if (dataset != null && dataset.Tables.Count > 0 && dataset.Tables[0].Rows.Count > 0)
                {
                    var row = dataset.Tables[0].Rows[0];
                    detail = new NewsDetailDto
                    {
                        PostId = Convert.ToInt64(row["PostId"]),
                        Title = row["Title"]?.ToString() ?? "",
                        Slug = row["Slug"] == DBNull.Value ? null : row["Slug"]?.ToString(),
                        Summary = row["Summary"] == DBNull.Value ? null : row["Summary"]?.ToString(),
                        ContentHtml = row["ContentHtml"] == DBNull.Value ? null : row["ContentHtml"]?.ToString(),
                        ThumbnailUrl = row["ThumbnailUrl"] == DBNull.Value ? null : row["ThumbnailUrl"]?.ToString(),
                        CategoryId = row["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["CategoryId"]),
                        CategoryName = row["CategoryName"]?.ToString() ?? "Tin tức chung",
                        ViewsCount = row["ViewsCount"] == DBNull.Value ? 1 : Convert.ToInt32(row["ViewsCount"]),
                        IsFeatured = string.Equals(row["IsFeatured"]?.ToString(), "Y", StringComparison.OrdinalIgnoreCase),
                        CreatedAt = parseDate(row["CreatedAt"])
                    };
                }

                if (dataset != null && dataset.Tables.Count > 1)
                {
                    foreach (DataRow r in dataset.Tables[1].Rows)
                    {
                        relatedList.Add(new NewsItemDto
                        {
                            PostId = Convert.ToInt64(r["PostId"]),
                            Title = r["Title"]?.ToString() ?? "",
                            Summary = r["Summary"] == DBNull.Value ? null : r["Summary"]?.ToString(),
                            ThumbnailUrl = r["ThumbnailUrl"] == DBNull.Value ? null : r["ThumbnailUrl"]?.ToString(),
                            CategoryName = r["CategoryName"]?.ToString() ?? "Tin tức",
                            CreatedAt = parseDate(r["CreatedAt"])
                        });
                    }
                }

                if (detail != null)
                {
                    detail.RelatedPosts = relatedList;
                }

                return new CResponseMessage
                {
                    Data = detail,
                    code = o_code.Value?.ToString() ?? (detail != null ? "200" : "404"),
                    message = o_message.Value?.ToString() ?? (detail != null ? "Thành công" : "Không tìm thấy bài viết"),
                    Success = detail != null
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi lấy chi tiết tin tức: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> GetCategories()
        {
            try
            {
                await EnsureNewsProcedureExistsAsync();
                await SeedSampleNews();

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT category_id AS CategoryId, category_name AS CategoryName, slug AS Slug FROM dbo.post_categories ORDER BY category_id ASC";
                using var cmd = new SqlCommand(sql, conn);
                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                var list = new List<NewsCategoryDto>();
                foreach (DataRow r in dt.Rows)
                {
                    list.Add(new NewsCategoryDto
                    {
                        CategoryId = Convert.ToInt32(r["CategoryId"]),
                        CategoryName = r["CategoryName"]?.ToString() ?? "",
                        Slug = r["Slug"] == DBNull.Value ? null : r["Slug"]?.ToString()
                    });
                }

                return new CResponseMessage
                {
                    Data = list,
                    code = "200",
                    message = "Thành công",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi lấy danh mục tin tức: " + ex.Message
                };
            }
        }
    }
}
