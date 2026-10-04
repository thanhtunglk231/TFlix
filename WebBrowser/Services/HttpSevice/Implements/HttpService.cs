    using CoreLib.Models;
    using Newtonsoft.Json;
    using Microsoft.Extensions.Logging;
    using System.Data;
    using System.Text;
    using WebBrowser.Services.HttpSevice.Interfaces;

    namespace WebBrowser.Services.HttpSevice.Implements
    {
        public class HttpService : IHttpService
        {
            private readonly HttpClient _client;
            private readonly HttpClient _longRunningClient;
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly ILogger<HttpService> _logger;

            public HttpService(IHttpClientFactory httpClientFactory, IConfiguration config, IHttpContextAccessor httpContextAccessor, ILogger<HttpService> logger)
            {
                _httpContextAccessor = httpContextAccessor;
                _logger = logger;
                _client = httpClientFactory.CreateClient();
                _client.BaseAddress = new Uri(config["PathStrings:Url"]);
                _longRunningClient = httpClientFactory.CreateClient("VideoUpload");
                _longRunningClient.BaseAddress = _client.BaseAddress;
            }

            // ===== Helpers logging =====
            private void Log(string message)
                => _logger.LogInformation("{HttpServiceMessage}", message);

            private void LogObject(string label, object? data)
                => _logger.LogDebug("{Operation} payload type {PayloadType}", label, data?.GetType().Name);

            private void AddBearerToken(HttpClient? client = null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string? token = null;

                if (httpContext != null)
                {
                    var path = httpContext.Request.Path.Value ?? "";
                    if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
                    {
                        token = httpContext.Session.GetString("AdminJWToken");
                    }

                    if (string.IsNullOrEmpty(token))
                    {
                        token = httpContext.Session.GetString("JWToken") ?? httpContext.Session.GetString("AdminJWToken");
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    (client ?? _client).DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }

            private StringContent CreateJsonContent(object data)
            {
                var json = JsonConvert.SerializeObject(data);
                return new StringContent(json, Encoding.UTF8, "application/json");
            }

            public async Task<DataTable> GetDataTableAsync(string url)
            {
                Log($"GetDataTableAsync -> {url}");
                AddBearerToken();

                var ds = await GetAsync<DataSet>(url);
                return ds?.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            }

            public async Task<DataRow> GetDataRowAsync(string url)
            {
                Log($"GetDataRowAsync -> {url}");
                var dt = await GetDataTableAsync(url);
                return dt.Rows.Count > 0 ? dt.Rows[0] : dt.NewRow();
            }

            public async Task<CResponseMessage> GetResponseAsync(string url)
            {
                Log($"GetResponseAsync -> {url}");
                return await GetAsync<CResponseMessage>(url);
            }

            public async Task<DataTable> PostDataTableAsync(string url, object data)
            {
                Log($"PostDataTableAsync -> {url}");
                LogObject("POST body", data);
                AddBearerToken();

                var ds = await PostAsync<DataSet>(url, data);
                return ds?.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            }

            public async Task<DataRow> PostDataRowAsync(string url, object data)
            {
                Log($"PostDataRowAsync -> {url}");
                LogObject("POST body", data);

                var dt = await PostDataTableAsync(url, data);
                return dt.Rows.Count > 0 ? dt.Rows[0] : dt.NewRow();
            }

            public async Task<CResponseMessage> PostResponseAsync(string url, object data)
            {
                Log($"PostResponseAsync -> {url}");
                LogObject("POST body", data);
                AddBearerToken();

                return await PostAsync<CResponseMessage>(url, data);
            }

            public async Task<T> GetAsync<T>(string url)
            {
                Log($"GetAsync<{typeof(T).Name}> -> {url}");
                AddBearerToken();

                try
                {
                    var response = await _client.GetAsync(url);
                    Log($"GET Status: {(int)response.StatusCode} {response.StatusCode}");

                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMsg = $"Lỗi API: {response.StatusCode}";
                        Log($"Error: {errorMsg}");
                        throw new Exception(errorMsg);
                    }

                    var result = JsonConvert.DeserializeObject<T>(json);
                    Log($"GET Deserialized -> {typeof(T).Name}");
                    return result!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP GET {Url} failed", url);
                    throw;
                }
            }

            public async Task<T> PostAsync<T>(string url, object data)
            {
                try
                {
                    Log($"PostAsync<{typeof(T).Name}> -> {url}");
                    LogObject("POST body", data);

                    AddBearerToken();
                    Log($"POST to: {new Uri(_client.BaseAddress!, url)}");

                    var jsonContent = CreateJsonContent(data);
                    var response = await _client.PostAsync(url, jsonContent);

                    Log($"POST Status: {(int)response.StatusCode} {response.StatusCode}");

                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<T>(json);
                    Log($"POST Deserialized -> {typeof(T).Name}");
                    return result!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP POST {Url} failed", url);

                    if (typeof(T) == typeof(CResponseMessage))
                    {
                        Log("Return fallback CResponseMessage due to exception.");
                        return (T)(object)new CResponseMessage
                        {
                            Success = false,
                            code = "500",
                            message = "Lỗi hệ thống: " + ex.Message
                        };
                    }

                    return default!;
                }
            }

            public async Task<T> PostLongRunningAsync<T>(string url, object data)
            {
                AddBearerToken(_longRunningClient);
                Log($"Long-running POST -> {url}");

                try
                {
                    using var content = CreateJsonContent(data);
                    using var response = await _longRunningClient.PostAsync(url, content);
                    var json = await response.Content.ReadAsStringAsync();
                    Log($"Long-running POST Status: {(int)response.StatusCode} {response.StatusCode}");

                    var result = JsonConvert.DeserializeObject<T>(json);
                    if (!response.IsSuccessStatusCode)
                        _logger.LogWarning("Long-running HTTP POST {Url} returned status {StatusCode}", url, (int)response.StatusCode);

                    return result!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Long-running HTTP POST {Url} failed", url);
                    if (typeof(T) == typeof(CResponseMessage))
                    {
                        return (T)(object)new CResponseMessage
                        {
                            Success = false,
                            code = "500",
                            message = "Lỗi xử lý upload video: " + ex.Message
                        };
                    }

                    return default!;
                }
            }

            public async Task<string> GetRawJsonAsync(string url)
            {
                Log($"GetRawJsonAsync -> {url}");
                AddBearerToken();

                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }

            public async Task<List<T>> GetListAsync<T>(string url)
            {
                Log($"GetListAsync<{typeof(T).Name}> -> {url}");
                return await GetAsync<List<T>>(url);
            }

            public async Task<DataSet> GetDataSetFromResponseAsync(string url)
            {
                Log($"GetDataSetFromResponseAsync -> {url}");
                AddBearerToken();

                try
                {
                    var response = await GetAsync<CResponseMessage>(url);
                    if (response?.Data != null)
                    {
                        var json = JsonConvert.SerializeObject(response.Data);
                        return JsonConvert.DeserializeObject<DataSet>(json) ?? new DataSet();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert API response from {Url} to a data set", url);
                }

                return new DataSet();
            }

            public async Task<CResponseMessage> PutResponseAsync(string url, object data)
            {
                Log($"PutResponseAsync -> {url}");
                LogObject("PUT body", data);

                AddBearerToken();

                try
                {
                    var jsonContent = CreateJsonContent(data ?? new { });

                    var response = await _client.PutAsync(url, jsonContent);
                    var json = await response.Content.ReadAsStringAsync();

                    Log($"PUT Status: {(int)response.StatusCode} {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return new CResponseMessage
                        {
                            Success = false,
                            code = ((int)response.StatusCode).ToString(),
                            message = "Cập nhật thất bại hoặc không có quyền."
                        };
                    }

                    var result = JsonConvert.DeserializeObject<CResponseMessage>(json);

                    return result ?? new CResponseMessage
                    {
                        Success = false,
                        code = "500",
                        message = "Không đọc được dữ liệu kết quả từ API"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP PUT {Url} failed", url);
                    return new CResponseMessage
                    {
                        Success = false,
                        code = "500",
                        message = "Lỗi gọi PUT API: " + ex.Message
                    };
                }
            }

            public async Task<List<T>> GetTableFromCResponseAsync<T>(string url)
            {
                Log($"GetTableFromCResponseAsync<{typeof(T).Name}> -> {url}");
                AddBearerToken();

                try
                {
                    var response = await GetAsync<CResponseMessage>(url);

                    if (response?.Data == null)
                        return new List<T>();

                    var json = JsonConvert.SerializeObject(response.Data);
                    var wrapper = JsonConvert.DeserializeObject<TableWrapper<T>>(json);

                    return wrapper?.Table ?? new List<T>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert API response from {Url} to a table", url);
                    return new List<T>();
                }
            }

            public async Task<CResponseMessage> DeleteWithBodyResponseAsync(string url, object data)
            {
                Log($"DeleteWithBodyResponseAsync -> {url}");
                AddBearerToken();

                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(_client.BaseAddress + url),
                        Content = CreateJsonContent(data)
                    };

                    var response = await _client.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    Log($"DELETE(body) Status: {(int)response.StatusCode} {response.StatusCode}");

                    var rootObj = JsonConvert.DeserializeObject<dynamic>(json);
                    var resultToken = rootObj?.result;

                    if (resultToken != null)
                    {
                        var resultStr = JsonConvert.SerializeObject(resultToken);
                        return JsonConvert.DeserializeObject<CResponseMessage>(resultStr);
                    }

                    return JsonConvert.DeserializeObject<CResponseMessage>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP DELETE with body to {Url} failed", url);
                    return new CResponseMessage
                    {
                        Success = false,
                        code = "500",
                        message = "Lỗi gọi DELETE API: " + ex.Message
                    };
                }
            }

            public async Task<CResponseMessage> DeleteResponseAsync(string url)
            {
                Log($"DeleteResponseAsync -> {url}");
                AddBearerToken();

                try
                {
                    var response = await _client.DeleteAsync(url);
                    var json = await response.Content.ReadAsStringAsync();

                    Log($"DELETE Status: {(int)response.StatusCode} {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return new CResponseMessage
                        {
                            Success = false,
                            code = ((int)response.StatusCode).ToString(),
                            message = "Xóa thất bại hoặc không có quyền."
                        };
                    }

                    var wrapper = JsonConvert.DeserializeObject<ApiResponseWrapper<CResponseMessage>>(json);
                    var result = wrapper?.result;

                    if (result == null)
                    {
                        return new CResponseMessage
                        {
                            Success = false,
                            code = "500",
                            message = "Không đọc được kết quả từ API."
                        };
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP DELETE {Url} failed", url);
                    return new CResponseMessage
                    {
                        Success = false,
                        code = "500",
                        message = "Lỗi gọi DELETE API: " + ex.Message
                    };
                }
            }
            public async Task<T> PostMultipartAsync<T>(string url, MultipartFormDataContent content)
            {
                Log($"PostMultipartAsync<{typeof(T).Name}> -> {url}");
                AddBearerToken();

                try
                {
                    Log($"POST multipart to: {new Uri(_client.BaseAddress!, url)}");
                    var response = await _client.PostAsync(url, content);

                    Log($"POST(multipart) Status: {(int)response.StatusCode} {response.StatusCode}");
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Lỗi API: {response.StatusCode}");

                    var result = JsonConvert.DeserializeObject<T>(json);
                    return result!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP multipart POST {Url} failed", url);
                    if (typeof(T) == typeof(CResponseMessage))
                    {
                        return (T)(object)new CResponseMessage
                        {
                            Success = false,
                            code = "500",
                            message = "Lỗi hệ thống: " + ex.Message
                        };
                    }
                    return default!;
                }
            }

            public async Task<T> PutMultipartAsync<T>(string url, MultipartFormDataContent content)
            {
                Log($"PutMultipartAsync<{typeof(T).Name}> -> {url}");
                AddBearerToken();

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
                    var response = await _client.SendAsync(request);

                    Log($"PUT(multipart) Status: {(int)response.StatusCode} {response.StatusCode}");
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Lỗi API: {response.StatusCode}");

                    var result = JsonConvert.DeserializeObject<T>(json);
                    return result!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP multipart PUT {Url} failed", url);
                    if (typeof(T) == typeof(CResponseMessage))
                    {
                        return (T)(object)new CResponseMessage
                        {
                            Success = false,
                            code = "500",
                            message = "Lỗi hệ thống: " + ex.Message
                        };
                    }
                    return default!;
                }
            }

            public class ApiResponseWrapper<T>
            {
                public T result { get; set; }
            }

            // Dùng cho GetTableFromCResponseAsync
            private class TableWrapper<T>
            {
                public List<T> Table { get; set; } = new();
            }
        }
    }
