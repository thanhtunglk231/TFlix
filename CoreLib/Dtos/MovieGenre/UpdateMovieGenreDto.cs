using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.MovieGenre
{
    public class UpdateMovieGenreDto
    {
        public decimal MovieId { get; set; }
        public decimal OldGenreId { get; set; }
        public decimal NewGenreId { get; set; }
    }
}
