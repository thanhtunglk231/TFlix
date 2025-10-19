using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLib.Dtos.Home
{
    public class MovieLatestItem
    {

        
        public int MovieId { get; set; }

       
        public string? Title { get; set; }

   
        public string? OriginalTitle { get; set; }

       
        public DateTime? ReleaseDate { get; set; }


        public string? Status { get; set; }

      
      
        public string? IsPremium { get; set; }

    
        public string? PosterUrl { get; set; }

  
        public string? BackdropUrl { get; set; }

     
        public int SourceCount { get; set; }

      
        public string? Genres { get; set; }

        public bool IsPremiumBool => string.Equals(IsPremium, "Y", StringComparison.OrdinalIgnoreCase);

    }
}
