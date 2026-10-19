using CoreLib.Dtos.VideoUploadRequest;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public class VideoUploadRequestService : IVideoUploadRequestService
{
    private readonly IHttpService _httpService;

    public VideoUploadRequestService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<CResponseMessage?> SubmitRequestAsync(CreateVideoUploadRequestDto dto)
    {
        return await _httpService.PostAsync<CResponseMessage>("/api/VideoUploadRequests/submit", dto);
    }

    public async Task<VideoUploadRequestDto?> GetStatusAsync(long userId)
    {
        var response = await _httpService.GetAsync<CResponseMessage>($"/api/VideoUploadRequests/status/{userId}");
        return ConvertData<VideoUploadRequestDto>(response);
    }

    public async Task<List<VideoUploadRequestDto>> GetAllAsync(string? status = null)
    {
        var url = string.IsNullOrEmpty(status) 
            ? "/api/VideoUploadRequests/all" 
            : $"/api/VideoUploadRequests/all?status={Uri.EscapeDataString(status)}";

        var response = await _httpService.GetAsync<CResponseMessage>(url);
        return ConvertData<List<VideoUploadRequestDto>>(response) ?? new();
    }

    public async Task<CResponseMessage?> ReviewAsync(ReviewVideoUploadRequestDto dto)
    {
        return await _httpService.PostAsync<CResponseMessage>("/api/VideoUploadRequests/review", dto);
    }

    private static T? ConvertData<T>(CResponseMessage? response)
    {
        if (response?.Success != true || response.Data == null) return default;

        try
        {
            var jsonString = JsonConvert.SerializeObject(response.Data);

            if (jsonString.TrimStart().StartsWith("{"))
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(jsonString);
                var tableToken = token["table"] ?? token["Table"] ?? token["TABLE"];
                if (tableToken != null)
                {
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(string))
                    {
                        return tableToken.ToObject<T>();
                    }

                    if (tableToken is Newtonsoft.Json.Linq.JArray jArr)
                    {
                        if (jArr.Count > 0)
                        {
                            return jArr[0].ToObject<T>();
                        }
                        return default;
                    }
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonString);
        }
        catch
        {
            return default;
        }
    }
}
