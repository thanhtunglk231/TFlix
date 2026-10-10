using System.Data;

namespace CoreLib.Dtos.VideSoure
{
    public sealed class VideoSourceCreateResultDto
    {
        public DataSet DataSet { get; set; } = new();
        public decimal? SourceId { get; set; }
    }
}
