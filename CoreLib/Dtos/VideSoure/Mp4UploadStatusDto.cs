namespace CoreLib.Dtos.VideSoure
{
    public sealed class Mp4UploadStatusDto
    {
        public Guid UploadId { get; set; }
        public bool ResumeAvailable { get; set; }
        public int TotalChunks { get; set; }
        public List<int> UploadedChunks { get; set; } = new();
        public VideoUploadProgressDto Progress { get; set; } = new("receiving-chunks", "Đang chờ dữ liệu upload", 0, 0);
    }
}