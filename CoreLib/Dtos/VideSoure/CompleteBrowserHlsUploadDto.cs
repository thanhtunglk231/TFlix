namespace CoreLib.Dtos.VideSoure
{
    public sealed class CompleteBrowserHlsUploadDto : AddVideoSourceInputDto
    {
        public decimal? SourceId { get; set; }
        public string? OldStreamUrl { get; set; }
        public Guid PlaylistFileId { get; set; }
        public List<BrowserHlsUploadFileDto> Files { get; set; } = new();
    }

    public sealed class BrowserHlsUploadFileDto
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
    }

    public sealed class BrowserHlsUploadStatusDto
    {
        public Guid UploadId { get; set; }
        public bool Exists { get; set; }
        public bool IsCompleting { get; set; }
        public Dictionary<Guid, List<int>> UploadedChunks { get; set; } = new();
    }
}
