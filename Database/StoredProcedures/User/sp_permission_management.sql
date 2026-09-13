SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.permissions
    (
        permission_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_permissions PRIMARY KEY,
        screen_code NVARCHAR(100) NOT NULL,
        permission_code NVARCHAR(20) NOT NULL,
        permission_name NVARCHAR(200) NOT NULL,
        created_at DATETIMEOFFSET NOT NULL CONSTRAINT DF_permissions_created_at DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT UQ_permissions_screen_action UNIQUE (screen_code, permission_code)
    );
END;
GO

IF OBJECT_ID(N'dbo.role_permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.role_permissions
    (
        role_id BIGINT NOT NULL,
        permission_id BIGINT NOT NULL,
        created_at DATETIMEOFFSET NOT NULL CONSTRAINT DF_role_permissions_created_at DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT PK_role_permissions PRIMARY KEY (role_id, permission_id),
        CONSTRAINT FK_role_permissions_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(role_id),
        CONSTRAINT FK_role_permissions_permissions FOREIGN KEY (permission_id) REFERENCES dbo.permissions(permission_id)
    );
END;
GO

DECLARE @Screens TABLE(screen_code NVARCHAR(100), screen_name NVARCHAR(200));

INSERT INTO @Screens(screen_code, screen_name)
VALUES
    (N'Home', N'Dashboard'),
    (N'Movie', N'Phim'),
    (N'MovieAsset', N'Tài nguyên phim'),
    (N'MovieGenre', N'Thể loại phim'),
    (N'Series', N'Series'),
    (N'SeriesGenre', N'Thể loại series'),
    (N'SeriesGenres', N'Thể loại series API'),
    (N'Season', N'Season'),
    (N'Esopide', N'Episode Admin'),
    (N'Episode', N'Episode API'),
    (N'EpisodeAsset', N'Tài nguyên episode'),
    (N'EpisodeAssert', N'Tài nguyên episode API'),
    (N'Genres', N'Thể loại'),
    (N'VideoSoure', N'Nguồn video Admin'),
    (N'VideoSources', N'Nguồn video API'),
    (N'Permission', N'Phân quyền'),
    (N'Permissions', N'Phân quyền API');

DECLARE @Actions TABLE(permission_code NVARCHAR(20), action_name NVARCHAR(100));

INSERT INTO @Actions(permission_code, action_name)
VALUES
    (N'View', N'Xem'),
    (N'Create', N'Thêm'),
    (N'Update', N'Sửa'),
    (N'Delete', N'Xóa');

INSERT INTO dbo.permissions(screen_code, permission_code, permission_name)
SELECT s.screen_code, a.permission_code, CONCAT(a.action_name, N' ', s.screen_name)
FROM @Screens s
CROSS JOIN @Actions a
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.permissions p
    WHERE p.screen_code = s.screen_code
      AND p.permission_code = a.permission_code
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_code = N'ADMIN')
BEGIN
    INSERT INTO dbo.roles(role_code, role_name)
    VALUES (N'ADMIN', N'Administrator');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_code = N'VIEWER')
BEGIN
    INSERT INTO dbo.roles(role_code, role_name)
    VALUES (N'VIEWER', N'Viewer');
END;

INSERT INTO dbo.role_permissions(role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM dbo.roles r
CROSS JOIN dbo.permissions p
WHERE r.role_code = N'ADMIN'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.role_permissions rp
      WHERE rp.role_id = r.role_id
        AND rp.permission_id = p.permission_id
  );

INSERT INTO dbo.role_permissions(role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM dbo.roles r
CROSS JOIN dbo.permissions p
WHERE r.role_code = N'VIEWER'
  AND p.permission_code = N'View'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.role_permissions rp
      WHERE rp.role_id = r.role_id
        AND rp.permission_id = p.permission_id
  );

IF EXISTS (SELECT 1 FROM dbo.app_users)
AND EXISTS (SELECT 1 FROM dbo.roles WHERE role_code = N'ADMIN')
AND NOT EXISTS (SELECT 1 FROM dbo.user_roles)
BEGIN
    INSERT INTO dbo.user_roles(user_id, role_id)
    SELECT TOP (1) u.user_id, r.role_id
    FROM dbo.app_users u
    CROSS JOIN dbo.roles r
    WHERE r.role_code = N'ADMIN'
    ORDER BY u.user_id;
END;
GO

IF OBJECT_ID(N'dbo.sp_permission_get_all', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_permission_get_all;
GO
CREATE PROCEDURE dbo.sp_permission_get_all
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        permission_id AS PermissionId,
        screen_code AS ScreenCode,
        permission_code AS PermissionCode,
        permission_name AS PermissionName
    FROM dbo.permissions
    ORDER BY screen_code,
             CASE permission_code
                 WHEN N'View' THEN 1
                 WHEN N'Create' THEN 2
                 WHEN N'Update' THEN 3
                 WHEN N'Delete' THEN 4
                 ELSE 99
             END;

    SET @o_code = N'200';
    SET @o_message = N'Lấy danh sách quyền thành công.';
END;
GO

IF OBJECT_ID(N'dbo.sp_permission_matrix_get_all', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_permission_matrix_get_all;
GO
CREATE PROCEDURE dbo.sp_permission_matrix_get_all
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        role_id AS RoleId,
        role_code AS RoleCode,
        role_name AS RoleName
    FROM dbo.roles
    ORDER BY role_code;

    SELECT
        permission_id AS PermissionId,
        screen_code AS ScreenCode,
        permission_code AS PermissionCode,
        permission_name AS PermissionName
    FROM dbo.permissions
    ORDER BY screen_code,
             CASE permission_code
                 WHEN N'View' THEN 1
                 WHEN N'Create' THEN 2
                 WHEN N'Update' THEN 3
                 WHEN N'Delete' THEN 4
                 ELSE 99
             END;

    SELECT
        role_id AS RoleId,
        permission_id AS PermissionId
    FROM dbo.role_permissions
    ORDER BY role_id, permission_id;

    SET @o_code = N'200';
    SET @o_message = N'Lấy ma trận phân quyền thành công.';
END;
GO

IF OBJECT_ID(N'dbo.sp_role_permission_set', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_role_permission_set;
GO
CREATE PROCEDURE dbo.sp_role_permission_set
    @p_role_id BIGINT,
    @p_permission_id BIGINT,
    @p_is_allowed BIT,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_id = @p_role_id)
    BEGIN
        SET @o_code = N'404';
        SET @o_message = N'Role không tồn tại.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.permissions WHERE permission_id = @p_permission_id)
    BEGIN
        SET @o_code = N'404';
        SET @o_message = N'Permission không tồn tại.';
        RETURN;
    END;

    IF @p_is_allowed = 1
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.role_permissions
            WHERE role_id = @p_role_id
              AND permission_id = @p_permission_id
        )
        BEGIN
            INSERT INTO dbo.role_permissions(role_id, permission_id)
            VALUES (@p_role_id, @p_permission_id);
        END;
    END
    ELSE
    BEGIN
        DELETE FROM dbo.role_permissions
        WHERE role_id = @p_role_id
          AND permission_id = @p_permission_id;
    END;

    SET @o_code = N'200';
    SET @o_message = N'Cập nhật phân quyền thành công.';
END;
GO

IF OBJECT_ID(N'dbo.sp_permission_get_by_user', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_permission_get_by_user;
GO
CREATE PROCEDURE dbo.sp_permission_get_by_user
    @p_email NVARCHAR(320),
    @p_screen_code NVARCHAR(100) = NULL,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.app_users WHERE email = @p_email AND status = N'ACTIVE')
    BEGIN
        SET @o_code = N'404';
        SET @o_message = N'User không tồn tại hoặc không hoạt động.';
        RETURN;
    END;

    SELECT
        p.screen_code AS ScreenCode,
        MAX(CASE WHEN p.permission_code = N'View' THEN 1 ELSE 0 END) AS CanView,
        MAX(CASE WHEN p.permission_code = N'Create' THEN 1 ELSE 0 END) AS CanCreate,
        MAX(CASE WHEN p.permission_code = N'Update' THEN 1 ELSE 0 END) AS CanUpdate,
        MAX(CASE WHEN p.permission_code = N'Delete' THEN 1 ELSE 0 END) AS CanDelete
    FROM dbo.app_users u
    INNER JOIN dbo.user_roles ur ON ur.user_id = u.user_id
    INNER JOIN dbo.role_permissions rp ON rp.role_id = ur.role_id
    INNER JOIN dbo.permissions p ON p.permission_id = rp.permission_id
    WHERE u.email = @p_email
      AND (@p_screen_code IS NULL OR p.screen_code = @p_screen_code)
    GROUP BY p.screen_code
    ORDER BY p.screen_code;

    SET @o_code = N'200';
    SET @o_message = N'Lấy quyền user thành công.';
END;
GO

IF OBJECT_ID(N'dbo.sp_permission_check_user', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_permission_check_user;
GO
CREATE PROCEDURE dbo.sp_permission_check_user
    @p_email NVARCHAR(320),
    @p_screen_code NVARCHAR(100),
    @p_permission_code NVARCHAR(20),
    @o_allowed BIT OUTPUT,
    @o_code NVARCHAR(10) OUTPUT,
    @o_message NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @o_allowed = 0;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.app_users u
        INNER JOIN dbo.user_roles ur ON ur.user_id = u.user_id
        INNER JOIN dbo.role_permissions rp ON rp.role_id = ur.role_id
        INNER JOIN dbo.permissions p ON p.permission_id = rp.permission_id
        WHERE u.email = @p_email
          AND u.status = N'ACTIVE'
          AND p.screen_code = @p_screen_code
          AND p.permission_code = @p_permission_code
    )
    BEGIN
        SET @o_allowed = 1;
    END;

    SET @o_code = N'200';
    SET @o_message = CASE WHEN @o_allowed = 1 THEN N'Có quyền.' ELSE N'Không đủ quyền.' END;
END;
GO
