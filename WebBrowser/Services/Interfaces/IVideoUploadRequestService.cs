using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces;

public interface IVideoUploadRequestService
{
    Task<CResponseMessage?> SubmitRequestAsync(CreateVideoUploadRequestDto dto);
    Task<VideoUploadRequestDto?> GetStatusAsync(long userId);
    Task<List<VideoUploadRequestDto>> GetAllAsync(string? status = null);
    Task<CResponseMessage?> ReviewAsync(ReviewVideoUploadRequestDto dto);
}
