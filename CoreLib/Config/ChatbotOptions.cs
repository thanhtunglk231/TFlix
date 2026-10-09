namespace CoreLib.Config
{
    public class ChatbotOptions
    {
        public const string SectionName = "Chatbot";

        public int MaxMessagesPerMinute { get; set; } = 10;
        public int HistoryExpirationMinutes { get; set; } = 1440;
        public int MaxHistoryMessages { get; set; } = 20;
        public int CacheExpirationMinutes { get; set; } = 30;
    }
}
