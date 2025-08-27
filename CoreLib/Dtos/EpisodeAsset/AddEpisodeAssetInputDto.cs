using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.EpisodeAsset
{
    public class AddEpisodeAssetInputDto
    {
        public int EpisodeId { get; set; }
        public string AssetType { get; set; } = default!;  // STILL|THUMB|TRAILER
        public int? SortOrder { get; set; } = 0;
    }
}
