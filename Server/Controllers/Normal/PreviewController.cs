using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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


        [HttpGet("series")]
        public async Task<IActionResult> Preview_series([FromQuery] int seriesID)
        {

            var result =  _preView.Get_All_Series(seriesID);
            return Ok(result);
        }

    }
}
