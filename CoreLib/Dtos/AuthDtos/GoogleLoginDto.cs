namespace CoreLib.Dtos.AuthDtos
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class GoogleIdentityDto
    {
        public string Subject { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
