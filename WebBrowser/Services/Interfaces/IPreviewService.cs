using CoreLib.Dtos.Preview;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Preview;

namespace WebBrowser.Services.Interfaces
{
    public interface IPreviewService
    {
        Task<ApiResponse<PreviewTableWrapper>> get_preview(GETCONTENTByID id);
    }
}