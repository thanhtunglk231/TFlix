namespace CoreLib.Config
{
    public sealed class OpenAiOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-6.1-sol";
        public string VisionModel { get; set; } = "gpt-6-luna";
        public string ImageModel { get; set; } = "gpt-image-2";
        public string ImageQuality { get; set; } = "low";
        public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
        public int TimeoutSeconds { get; set; } = 60;
    }
}
