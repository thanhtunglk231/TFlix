using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.MovieAsset
{
    public class AddMovieAssetDto
    {
        [Required]
        public string OwnerType { get; set; } = "MOVIE";

        [Range(1, long.MaxValue)]
        public long OwnerId { get; set; }

        public decimal MovieId { get; set; }

        /// <summary>POSTER | BACKDROP | TRAILER | THUMB</summary>
      
        public string AssetType { get; set; } = string.Empty;

      
        public string Url { get; set; } = string.Empty;

       
        public decimal SortOrder { get; set; } = 0;
    }
}
