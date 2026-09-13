namespace CoreLib.Dtos
{
    public class PermissionDto
    {
        public long PermissionId { get; set; }
        public string ScreenCode { get; set; } = string.Empty;
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
    }

    public class UserPermissionDto
    {
        public string ScreenCode { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }

    public class RolePermissionSetDto
    {
        public long RoleId { get; set; }
        public long PermissionId { get; set; }
        public bool IsAllowed { get; set; }
    }

    public class CheckPermissionDto
    {
        public string Email { get; set; } = string.Empty;
        public string ScreenCode { get; set; } = string.Empty;
        public string PermissionCode { get; set; } = string.Empty;
    }
}
