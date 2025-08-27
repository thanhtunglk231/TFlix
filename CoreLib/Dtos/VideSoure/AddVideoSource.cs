using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.VideSoure
{
    
        public class AddVideoSourceDto
        {
            public decimal? MovieId { get; set; }          // null nếu là episode
            public decimal? EpisodeId { get; set; }        // null nếu là movie

            public string Provider { get; set; } = string.Empty;
            public string? ServerName { get; set; }
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
        public string StreamUrl { get; set; } = string.Empty;

            public string? Quality { get; set; }       // 360p/720p/1080p/4K...
            public string? Format { get; set; }        // HLS/DASH/MP4...
            public string? DrmType { get; set; }
            public string? DrmLicenseUrl { get; set; }

            public bool IsPrimary { get; set; }
            public string Status { get; set; } = "ACTIVE";  // ACTIVE / DISABLED
        }

        // DTO cập nhật kế thừa từ Add (thêm khoá chính)
        // Lưu ý: sp_video_source_update không cho đổi movie_id/episode_id,
        // nên nếu bạn truyền MovieId/EpisodeId ở đây thì cũng sẽ bị bỏ qua.
        public class UpdateVideoSourceDto : AddVideoSourceDto
        {
            public decimal SourceId { get; set; }      // PK cần để update
        }
    

}
