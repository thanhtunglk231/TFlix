namespace CoreLib.Config
{
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.5-flash";
        public List<string> Models { get; set; } = new();
        public string TranscriptionModel { get; set; } = "gemini-3.5-transcribe";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public int TimeoutSeconds { get; set; } = 180;
    }
}
