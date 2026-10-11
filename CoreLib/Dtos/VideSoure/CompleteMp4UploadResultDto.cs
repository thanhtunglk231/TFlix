namespace CoreLib.Dtos.VideSoure
{
    public class CompleteMp4UploadResultDto : CoreLib.Models.CResponseMessage
    {
        public string? PlaylistUrl { get; set; }
        public decimal? SourceId { get; set; }
        public int? SegmentsCount { get; set; }
    }
}
