using CoreLib.Dtos.Comment;
using CoreLib.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class CommentService : ICommentService
    {
        private readonly IHttpService _httpService;

        public CommentService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<CommentDto>> GetCommentsByContentAsync(long? movieId, long? episodeId)
        {
            try
            {
                string url = $"/api/Comment/by-content?movieId={(movieId.HasValue ? movieId.Value.ToString() : "")}&episodeId={(episodeId.HasValue ? episodeId.Value.ToString() : "")}";
                var response = await _httpService.GetAsync<CResponseMessage>(url);
                if (response != null && response.Data != null)
                {
                    var json = JsonConvert.SerializeObject(response.Data);
                    var list = JsonConvert.DeserializeObject<List<CommentDto>>(json);
                    return list ?? new List<CommentDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CommentService.GetCommentsByContentAsync] Error: " + ex.Message);
            }
            return new List<CommentDto>();
        }

        public async Task<CommentDto?> AddCommentAsync(CreateCommentDto request)
        {
            try
            {
                var response = await _httpService.PostAsync<CResponseMessage>("/api/Comment/add", request);
                if (response != null && response.Data != null)
                {
                    var json = JsonConvert.SerializeObject(response.Data);
                    var item = JsonConvert.DeserializeObject<CommentDto>(json);
                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CommentService.AddCommentAsync] Error: " + ex.Message);
            }
            return null;
        }
    }
}
