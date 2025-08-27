namespace WebBrowser.Models
{
    public class ApiResponse<T>
    {
        public string code { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
        public T Data { get; set; }
    }
}
