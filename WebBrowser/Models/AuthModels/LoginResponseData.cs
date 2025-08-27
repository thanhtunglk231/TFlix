namespace WebBrowser.Models.AuthModels
{
    public class LoginResponseData
    {
        public string token { get; set; } = "";
        public UserInfo user { get; set; } = new();
    }
}
