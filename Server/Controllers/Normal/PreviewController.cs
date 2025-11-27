using CoreLib.Dtos.Preview;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Server.Controllers.Normal
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewController : ControllerBase
    {
        private readonly IPreView _preView;
        public PreviewController(IPreView preView) { 
        
        _preView = preView;
        }

        [HttpGet("movie")]
        public async Task<IActionResult> Preview_movie([FromQuery] int movieID) { 
        
        var result = _preView.get_all(movieID);
        return Ok(result);
        }


        [HttpGet("GetPreview")]
        public IActionResult preview([FromQuery] GETCONTENTByID movie)
        {
            Console.WriteLine("=== [Preview] Incoming Query ===");
            Console.WriteLine($"movie.id   = {movie?.id}");
            Console.WriteLine($"movie.kind = {movie?.kind}");

            var result = _preView.GET_CONTENT_BY_ID(movie);

            Console.WriteLine("=== [Preview] Result From Service ===");
            Console.WriteLine($"Result Code   = {result.code}");
            Console.WriteLine($"Success       = {result.Success}");
            Console.WriteLine($"Message       = {result.message}");
            Console.WriteLine($"Table count   = {result.Data}");


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
