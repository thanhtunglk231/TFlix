using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.MovieGenre
{
    public class DeleteMovieGenreDto
    {
        public decimal MovieId { get; set; }
        public decimal GenreId { get; set; }
    }
}
