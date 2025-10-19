using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.SeriesGenre
{
    public class DeleteSeriesGenreDto
    {
        public decimal SeriesId { get; set; }
        public decimal GenreId { get; set; }
    }
}
