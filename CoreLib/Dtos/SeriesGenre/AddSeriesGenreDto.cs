using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.SeriesGenre
{
    public class AddSeriesGenreDto
    {
        public int SeriesId { get; set; }
        public int GenreId { get; set; }
    }
}
