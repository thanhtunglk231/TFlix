namespace CoreLib.Dtos.AuthDtos
{
    public class OtpLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
