namespace CoreLib.Dtos.VideSoure
{
    public class CompleteMp4VideoUploadDto : AddVideoSourceInputDto
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public decimal? SourceId { get; set; }
        public string? OldStreamUrl { get; set; }
    }
}