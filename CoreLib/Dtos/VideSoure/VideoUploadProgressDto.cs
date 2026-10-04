namespace CoreLib.Dtos.VideSoure
{
    public sealed record VideoUploadProgressDto(
        string Stage,
        string Message,
        int SegmentsUploaded,
        int TotalSegments)
    {
        public int Percent => TotalSegments <= 0
            ? 0
            : Math.Clamp((int)Math.Floor(SegmentsUploaded * 100d / TotalSegments), 0, 100);
    }
}