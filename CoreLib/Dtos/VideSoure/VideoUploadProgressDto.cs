namespace CoreLib.Dtos.VideSoure
{
    public sealed record VideoUploadProgressDto(
        string Stage,
        string Message,
        int SegmentsUploaded,
        int TotalSegments,
        long BytesUploaded = 0,
        long TotalBytes = 0,
        int StagePercent = 0)
    {
        public int StoragePercent => TotalBytes > 0
            ? Math.Clamp((int)Math.Floor(BytesUploaded * 100d / TotalBytes), 0, 100)
            : TotalSegments <= 0
                ? 0
                : Math.Clamp((int)Math.Floor(SegmentsUploaded * 100d / TotalSegments), 0, 100);

        public int Percent => Stage switch
        {
            "receiving-chunks" => Math.Clamp(StagePercent, 0, 30),
            "assembling" => 35,
            "packaging" => 35 + (int)Math.Floor(Math.Clamp(StagePercent, 0, 100) * 0.25d),
            "uploading-segments" => 60 + (int)Math.Floor(StoragePercent * 0.35d),
            "saving" => 97,
            "complete" => 100,
            _ => 0
        };

        public int RemainingPercent => Math.Clamp(100 - Percent, 0, 100);
        public long RemainingBytes => Math.Max(TotalBytes - BytesUploaded, 0);
    }
}
