-- =================================================================
-- Stored Procedures for CMS / News Module (Tin tức & Bài viết)
-- Date: 2026-09-13
-- =================================================================

-- 1. Ensure Table post_categories
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[post_categories]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.post_categories (
        category_id INT IDENTITY(1,1) PRIMARY KEY,
        category_name NVARCHAR(200) NOT NULL,
        slug NVARCHAR(200) NULL,
        created_at DATETIME DEFAULT GETDATE()
    );
END;

-- 2. Ensure Table posts
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
        created_at DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_posts_categories FOREIGN KEY (category_id) REFERENCES dbo.post_categories(category_id) ON DELETE SET NULL
    );
END;

-- 3. Stored Procedure: sp_news_catalog_search
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_news_catalog_search]') AND type in (N'P', N'PC'))
    DROP PROCEDURE dbo.sp_news_catalog_search;
GO

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

        SET @p_search = NULLIF(TRIM(@p_search), N'');

        -- Count Total
        SELECT @o_total_count = COUNT(1)
        FROM dbo.posts p
        WHERE (p.status IS NULL OR p.status = N'PUBLISHED')
          AND (@p_search IS NULL OR p.title LIKE N'%' + @p_search + N'%' OR p.summary LIKE N'%' + @p_search + N'%')
          AND (@p_category_id IS NULL OR p.category_id = @p_category_id);

        -- Select Page Results
        SELECT 
            p.post_id           AS PostId,
            p.title             AS Title,
            p.slug              AS Slug,
            p.summary           AS Summary,
            COALESCE(p.thumbnail_url, N'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop') AS ThumbnailUrl,
            p.category_id       AS CategoryId,
            COALESCE(c.category_name, N'Tin tức chung') AS CategoryName,
            COALESCE(p.views_count, 0) AS ViewsCount,
            COALESCE(p.is_featured, N'N') AS IsFeatured,
            p.created_at        AS CreatedAt
        FROM dbo.posts p
        LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
        WHERE (p.status IS NULL OR p.status = N'PUBLISHED')
          AND (@p_search IS NULL OR p.title LIKE N'%' + @p_search + N'%' OR p.summary LIKE N'%' + @p_search + N'%')
          AND (@p_category_id IS NULL OR p.category_id = @p_category_id)
        ORDER BY p.is_featured DESC, p.created_at DESC
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

-- 4. Stored Procedure: sp_news_get_by_id
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_news_get_by_id]') AND type in (N'P', N'PC'))
    DROP PROCEDURE dbo.sp_news_get_by_id;
GO

CREATE PROCEDURE dbo.sp_news_get_by_id
    @p_post_id      BIGINT,
    @o_code         NVARCHAR(10) OUTPUT,
    @o_message      NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Increment view count
        UPDATE dbo.posts
        SET views_count = ISNULL(views_count, 0) + 1
        WHERE post_id = @p_post_id;

        -- ResultSet 1: Post Detail
        SELECT 
            p.post_id           AS PostId,
            p.title             AS Title,
            p.slug              AS Slug,
            p.summary           AS Summary,
            p.content_html      AS ContentHtml,
            COALESCE(p.thumbnail_url, N'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop') AS ThumbnailUrl,
            p.category_id       AS CategoryId,
            COALESCE(c.category_name, N'Tin tức chung') AS CategoryName,
            COALESCE(p.views_count, 1) AS ViewsCount,
            COALESCE(p.is_featured, N'N') AS IsFeatured,
            p.created_at        AS CreatedAt
        FROM dbo.posts p
        LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
        WHERE p.post_id = @p_post_id;

        -- ResultSet 2: Related Posts (Top 4)
        SELECT TOP 4
            p.post_id           AS PostId,
            p.title             AS Title,
            p.summary           AS Summary,
            COALESCE(p.thumbnail_url, N'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600&auto=format&fit=crop') AS ThumbnailUrl,
            COALESCE(c.category_name, N'Tin tức') AS CategoryName,
            p.created_at        AS CreatedAt
        FROM dbo.posts p
        LEFT JOIN dbo.post_categories c ON p.category_id = c.category_id
        WHERE p.post_id <> @p_post_id
          AND (p.status IS NULL OR p.status = N'PUBLISHED')
        ORDER BY p.created_at DESC;

        SET @o_code = N'200';
        SET @o_message = N'Thành công';
    END TRY
    BEGIN CATCH
        SET @o_code = N'500';
        SET @o_message = ERROR_MESSAGE();
    END CATCH
END;
GO
