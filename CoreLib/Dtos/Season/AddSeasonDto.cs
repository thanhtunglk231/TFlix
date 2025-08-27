using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.Season
{
    
        public class AddSeasonDto
        {
            public decimal SeriesId { get; set; }          // FK series
            public int SeasonNo { get; set; }          // UNIQUE(series_id, season_no)
            public string? Title { get; set; }
            public string? Overview { get; set; }
            public DateTime? AirDate { get; set; }
        }

        public class UpdateSeasonDto : AddSeasonDto
        {
            public decimal SeasonId { get; set; }          // PK seasons
        }
    

}
