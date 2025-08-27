using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.EpisodeAsset
{
    public class AddEpisodeAsset
    {
        public decimal EpisodeId { get; set; }

        /// <summary>
        /// STILL | THUMB | TRAILER
        /// </summary>
        public string AssetType { get; set; } = null!;

        /// <summary>
        /// Public URL đã upload lên Supabase.
        /// </summary>
        public string Url { get; set; } = null!;

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// Dùng khi cập nhật asset (thay URL, đổi loại, sort order…).
    /// </summary>
    public class UpdateEpisodeAsset : AddEpisodeAsset
    {
        /// <summary>
        /// Khóa chính asset_id (NUMBER trong Oracle).
        /// </summary>
        public decimal AssetId { get; set; }
    }
}

