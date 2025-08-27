using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.MovieAsset
{
    public class UpdateMovieAssetDto
    {
        [Required]
  
        public decimal AssetId { get; set; }

        /// <summary>POSTER | BACKDROP | TRAILER | THUMB</summary>

        public string AssetType { get; set; } = string.Empty;

        public decimal MovieId { get; set; }

        public string Url { get; set; } = string.Empty;

        
        public int SortOrder { get; set; } = 0;
    }
}
