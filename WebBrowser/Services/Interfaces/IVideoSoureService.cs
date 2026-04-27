using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.VideoSoure;

namespace WebBrowser.Services.Interfaces
{
    public interface IVideoSoureService
    {
        Task<CResponseMessage> add_VideoSoure(IFormFile file, AddVideoSourceInputDto addVideoSourceDto);
        Task<ApiResponse<SourceTableWrapper>> get_all();
        Task<CResponseMessage> uppdate_VideoSoure(decimal sourceId, IFormFile file, [FromBody] UpdateVideoSourceInputDto meta);
    }
}