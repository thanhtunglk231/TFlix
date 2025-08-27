using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.VideSoure
{
    // Chỉ dùng ở Controller khi nhận request
    public class AddVideoSourceInputDto
    {
        public int? MovieId { get; set; }
        public int? EpisodeId { get; set; }

        public string Provider { get; set; } = "SUPABASE";
        public string? ServerName { get; set; } = "cdn-1";
        public string? Quality { get; set; }   // 1080p...
        public string? Format { get; set; }    // MP4/HLS/DASH
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; } = true;
        public string Status { get; set; } = "ACTIVE";
    }

    public class UpdateVideoSourceInputDto : AddVideoSourceInputDto
    {
        public decimal SourceId { get; set; }
        // gửi kèm URL cũ để server xoá sau khi update OK (optional)
        public string? OldStreamUrl { get; set; }
    }

}
