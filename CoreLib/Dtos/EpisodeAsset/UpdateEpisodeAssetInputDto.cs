using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.EpisodeAsset
{
    public class UpdateEpisodeAssetInputDto
    {
        public int EpisodeId { get; set; }
        public string AssetType { get; set; } = default!;
        public int? SortOrder { get; set; } = 0;

        // nếu client biết URL cũ thì gửi để server xoá file cũ sau khi cập nhật
        public string? OldUrl { get; set; }
    }
}
