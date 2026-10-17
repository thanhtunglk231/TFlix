using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Subtitle;
using Microsoft.AspNetCore.Mvc;
using Server.Services.Subtitles;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubtitlesController : ControllerBase
    {
        private readonly ISubtitleService _subtitleService;

        public SubtitlesController(ISubtitleService subtitleService)
        {
            _subtitleService = subtitleService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateSubtitleRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _subtitleService.GetOrGenerateSubtitlesAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] long? movieId,
            [FromQuery] long? episodeId,
            [FromQuery] string? title,
            [FromQuery] string? overview,
            [FromQuery] double? durationSeconds,
            CancellationToken cancellationToken)
        {
            var request = new GenerateSubtitleRequestDto
            {
                MovieId = movieId,
                EpisodeId = episodeId,
                Title = title,
                Overview = overview,
                DurationSeconds = durationSeconds,
                ForceRegenerate = false
            };

            var result = await _subtitleService.GetOrGenerateSubtitlesAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("vtt")]
        public async Task<IActionResult> GetWebVtt(
            [FromQuery] long? movieId,
            [FromQuery] long? episodeId,
            [FromQuery] string? lang,
            CancellationToken cancellationToken)
        {
            var vtt = await _subtitleService.GetWebVttContentAsync(movieId, episodeId, lang ?? "vi", cancellationToken);
            return Content(vtt, "text/vtt; charset=utf-8");
        }
    }
}
