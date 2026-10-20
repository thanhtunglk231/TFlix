using CoreLib.Dtos.Preview;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CoreLib.Models;
using Server.Services;

namespace Server.Controllers.Normal
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewController : ControllerBase
    {
        private readonly IPreView _preView;
        private readonly IRedisCacheService _cache;
        public PreviewController(IPreView preView, IRedisCacheService cache) {
        
        _preView = preView;
        _cache = cache;
        }

        [HttpGet("movie")]
        public async Task<IActionResult> Preview_movie([FromQuery] int movieID) { 
        
        var cacheKey = $"tflix:movies:preview:{movieID}";
        var result = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? _preView.get_all(movieID);
        if (result.Success)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return Ok(result);
        }


        [HttpGet("GetPreview")]
        public async Task<IActionResult> preview([FromQuery] GETCONTENTByID movie)
        {
            if (movie == null) return BadRequest("Thiếu thông tin truy vấn.");
            var kind = string.IsNullOrWhiteSpace(movie.kind) ? "movie" : movie.kind.Trim().ToLowerInvariant();
            var cacheKey = $"tflix:movies:content:{kind}:{movie.id}";
            var result = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? _preView.GET_CONTENT_BY_ID(movie);
            if (result.Success)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        

            return Ok(result);
        }


        [HttpGet("series")]
        public async Task<IActionResult> Preview_series([FromQuery] int seriesID)
        {

            var result =  _preView.Get_All_Series(seriesID);
            return Ok(result);
        }

    }
}
