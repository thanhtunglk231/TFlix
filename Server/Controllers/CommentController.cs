using CoreLib.Dtos.Comment;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICComment _commentDataService;

        public CommentController(ICComment commentDataService)
        {
            _commentDataService = commentDataService;
        }

        [HttpGet("by-content")]
        public async Task<IActionResult> GetByContent([FromQuery] long? movieId, [FromQuery] long? episodeId)
        {
            var result = await _commentDataService.GetCommentsByContentAsync(movieId, episodeId);
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CreateCommentDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Nội dung bình luận không được để trống."
                });
            }

            var result = await _commentDataService.AddCommentAsync(request);
            return Ok(result);
        }
    }
}
