using CoreLib.Models;
using System.Data;

namespace WebBrowser.Services.HttpSevice.Interfaces
{
    public interface IHttpService
    {
        Task<T> PostMultipartAsync<T>(string url, MultipartFormDataContent content);
        Task<T> PutMultipartAsync<T>(string url, MultipartFormDataContent content);
        Task<CResponseMessage> DeleteResponseAsync(string url);
        Task<CResponseMessage> DeleteWithBodyResponseAsync(string url, object data);
        Task<T> GetAsync<T>(string url);
        Task<DataRow> GetDataRowAsync(string url);
        Task<DataSet> GetDataSetFromResponseAsync(string url);
        Task<DataTable> GetDataTableAsync(string url);
        Task<List<T>> GetListAsync<T>(string url);
        Task<string> GetRawJsonAsync(string url);
        Task<CResponseMessage> GetResponseAsync(string url);
        Task<List<T>> GetTableFromCResponseAsync<T>(string url);
        Task<T> PostAsync<T>(string url, object data);
        Task<DataRow> PostDataRowAsync(string url, object data);
        Task<DataTable> PostDataTableAsync(string url, object data);
        Task<CResponseMessage> PostResponseAsync(string url, object data);
        Task<CResponseMessage> PutResponseAsync(string url, object data);
    }
}