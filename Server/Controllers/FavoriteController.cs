using CoreLib.Dtos.Favorite;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoriteController : ControllerBase
{
    private readonly ICFavorite _favorite;

    public FavoriteController(ICFavorite favorite) => _favorite = favorite;

    [HttpGet("movies")]
    public async Task<IActionResult> GetMovies([FromQuery] long userId) => Ok(await _favorite.GetMoviesAsync(userId));

    [HttpGet("movie-ids")]
    public async Task<IActionResult> GetMovieIds([FromQuery] long userId) => Ok(await _favorite.GetMovieIdsAsync(userId));

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] FavoriteMovieRequest request)
    {
        if (request.UserId <= 0 || request.MovieId <= 0)
            return BadRequest(new CResponseMessage { Success = false, code = "400", message = "Tài khoản hoặc phim không hợp lệ." });

        return Ok(await _favorite.ToggleMovieAsync(request));
    }
}
