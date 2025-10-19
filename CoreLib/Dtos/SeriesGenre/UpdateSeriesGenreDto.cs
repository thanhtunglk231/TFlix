using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.SeriesGenre
{
    public class UpdateSeriesGenreDto
    {
        public decimal SeriesId { get; set; }
        public decimal OldGenreId { get; set; }
        public decimal NewGenreId { get; set; }
    }
}
