using Microsoft.AspNetCore.Authentication.Cookies;
using CommonLib.Logging;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using WebBrowser.Services.Implements;
using WebBrowser.Services.Implements.Episodes;
using WebBrowser.Services.Implements.Movies;
using WebBrowser.Services.Implements.Series;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình File Logger ghi lỗi ra file txt
builder.Logging.AddFileLogger(options =>
{
    options.LogDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
    options.FileNamePrefix = "web";
    options.MinLevel = LogLevel.Information;
    options.RetainDays = 30;
});

// ? ??ng ký HttpClientFactory (fix l?i IHttpClientFactory)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("VideoUpload", client => client.Timeout = TimeSpan.FromHours(8));

// ? DI cho services
builder.Services.AddScoped<WebBrowser.Services.HttpSevice.Interfaces.IHttpService,
                           WebBrowser.Services.HttpSevice.Implements.HttpService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IAuthService,
                           WebBrowser.Services.Implements.AuthService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISeriesService,
                           SeriesService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISesonService,
                           WebBrowser.Services.Implements.SesonService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IMovieService,
                           MovieService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IEpisode,
                           Episode>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IVideoSoureService,
                           WebBrowser.Services.Implements.VideoSoureService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IEpisodeAsset,
                           EpisodeAsset>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IMovieAssetService,
                           MovieAssetService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IGenresService,
                           WebBrowser.Services.Implements.GenresService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IMovieGenreService,
                           MovieGenreService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISerireGenreService,
                           SerireGenreService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IHomeService,
                           HomeService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IPreviewService,
                           PreviewService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.INewsService,
                           NewsService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ICommentService,
                           WebBrowser.Services.Implements.CommentService>();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = "/auth/index";          // Trang login (view)
        opts.LogoutPath = "/auth/logout";
        opts.AccessDeniedPath = "/auth/index";
        opts.ExpireTimeSpan = TimeSpan.FromHours(1);
        opts.SlidingExpiration = true;
        // Optional: tên cookie
        // opts.Cookie.Name = "tflix.auth";
    });

// (Optional) Chính sách phân quyền theo role
builder.Services.AddAuthorization(opts =>
{
    // ví dụ: chính sách chỉ cho Admin
    opts.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});
// Các d?ch v? khác
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();
var requestLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ControllerRequest");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
        logger.LogError(ex, "Unhandled exception in WebBrowser");
        throw;
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    //app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
    if (action == null)
    {
        await next();
        return;
    }

    var stopwatch = Stopwatch.StartNew();
    var statusCode = StatusCodes.Status500InternalServerError;
    try
    {
        await next();
        statusCode = context.Response.StatusCode;
    }
    finally
    {
        stopwatch.Stop();
        requestLogger.LogInformation(
            "Controller {Controller}.{Action} handled {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds} ms",
            action.ControllerName,
            action.ActionName,
            context.Request.Method,
            context.Request.Path,
            statusCode,
            stopwatch.ElapsedMilliseconds);
    }
});

// ?? NH? b?t Session trong pipeline
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<WebBrowser.Hubs.CommentHub>("/commentHub");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
