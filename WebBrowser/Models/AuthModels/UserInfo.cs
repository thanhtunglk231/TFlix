namespace WebBrowser.Models.AuthModels
{
    public class UserInfo
    {
        public int userId { get; set; }
        public string email { get; set; } = "";
        public string fullName { get; set; } = "";
        public string avatarUrl { get; set; } = "";
        public string phone { get; set; } = "";
        public string countryCode { get; set; } = "";
        public string languageCode { get; set; } = "";
        public bool isEmailVerified { get; set; }
        public string status { get; set; } = "";
    }
}
