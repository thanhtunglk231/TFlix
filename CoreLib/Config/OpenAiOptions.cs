namespace CoreLib.Config
{
    public sealed class OpenAiOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-6.1-sol";
        public int TimeoutSeconds { get; set; } = 60;
    }
}
