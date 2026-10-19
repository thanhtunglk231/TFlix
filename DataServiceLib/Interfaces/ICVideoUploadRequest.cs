using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICVideoUploadRequest
{
    Task<CResponseMessage> SubmitRequestAsync(CreateVideoUploadRequestDto dto);
    Task<CResponseMessage> GetUserStatusAsync(long userId);
    Task<CResponseMessage> GetAllAsync(string? status);
    Task<CResponseMessage> ReviewRequestAsync(ReviewVideoUploadRequestDto dto);
}
