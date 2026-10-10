using CoreLib.Dtos.Rating;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
    private readonly ICRating _rating;

    public RatingController(ICRating rating)
    {
        _rating = rating;
    }

    [HttpGet("movie")]
    public async Task<IActionResult> GetMovieRating([FromQuery] long movieId, [FromQuery] long? userId = null)
    {
        if (movieId <= 0)
        {
            return BadRequest(new CResponseMessage
            {
                Success = false,
                code = "400",
                message = "Mã phim không hợp lệ."
            });
        }

        return Ok(await _rating.GetMovieRatingAsync(movieId, userId));
    }

    [HttpPost("rate")]
    public async Task<IActionResult> SetMovieRating([FromBody] SetMovieRatingRequest request)
    {
        if (request.UserId <= 0 || request.MovieId <= 0 || request.RatingVal < 1 || request.RatingVal > 5)
        {
            return BadRequest(new CResponseMessage
            {
                Success = false,
                code = "400",
                message = "Thông tin đánh giá không hợp lệ (Điểm từ 1 đến 5 sao)."
            });
        }

        return Ok(await _rating.SetMovieRatingAsync(request));
    }
}
