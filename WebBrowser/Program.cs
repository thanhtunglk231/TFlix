using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using CommonLib.Logging;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using WebBrowser.Services.Implements;
using WebBrowser.Services.Implements.Episodes;
using WebBrowser.Services.Implements.Movies;
using WebBrowser.Services.Implements.Series;

var builder = WebApplication.CreateBuilder(args);

// Giữ khóa mã hóa cookie/session ổn định qua các lần restart hoặc publish lại.
// Có thể cấu hình đường dẫn bên ngoài thư mục publish bằng DataProtection:KeysPath.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
}

Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("TFlix.WebBrowser");

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
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IPermissionService,
                           WebBrowser.Services.Implements.PermissionService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IAdminAccountService,
                           WebBrowser.Services.Implements.AdminAccountService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISubscriptionPlanService,
                           WebBrowser.Services.Implements.SubscriptionPlanService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IAccountSubscriptionService,
                           WebBrowser.Services.Implements.AccountSubscriptionService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IPaymentService,
                           WebBrowser.Services.Implements.PaymentService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IPreviewService,
                           PreviewService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.INewsService,
                           NewsService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ICommentService,
                           WebBrowser.Services.Implements.CommentService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IFavoriteService,
                           WebBrowser.Services.Implements.FavoriteService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IChatService,
                           WebBrowser.Services.Implements.ChatService>();
builder.Services.AddSignalR();
var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = "/auth/index";          // Trang login (view)
        opts.LogoutPath = "/auth/logout";
        opts.AccessDeniedPath = "/auth/access-denied";
        opts.ExpireTimeSpan = TimeSpan.FromHours(1);
        opts.SlidingExpiration = true;
        // Optional: tên cookie
        // opts.Cookie.Name = "tflix.auth";
    })
    .AddCookie("GoogleExternal", opts =>
    {
        opts.Cookie.Name = "tflix.google.external";
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? builder.Configuration["Google:ClientId"]
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? builder.Configuration["Google:ClientSecret"]
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var googleCallbackPath = builder.Configuration["Authentication:Google:CallbackPath"]
    ?? builder.Configuration["Google:CallbackPath"]
    ?? Environment.GetEnvironmentVariable("GOOGLE_CALLBACK_PATH")
    ?? "/signin-google";

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Authentication:Google:ClientId"] = googleClientId,
    ["Authentication:Google:ClientSecret"] = googleClientSecret,
    ["Authentication:Google:CallbackPath"] = googleCallbackPath
});
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle("Google", opts =>
    {
        opts.SignInScheme = "GoogleExternal";
        opts.ClientId = googleClientId;
        opts.ClientSecret = googleClientSecret;
        opts.CallbackPath = googleCallbackPath;
        opts.SaveTokens = true;
        opts.Scope.Clear();
        opts.Scope.Add("openid"); opts.Scope.Add("email"); opts.Scope.Add("profile");
        opts.Events.OnCreatingTicket = context =>
        {
            if (context.TokenResponse.Response.RootElement.TryGetProperty("id_token", out var idTokenElement))
            {
                var idToken = idTokenElement.GetString();
                if (!string.IsNullOrWhiteSpace(idToken))
                {
                    var tokens = context.Properties.GetTokens().ToList();
                    tokens.Add(new AuthenticationToken { Name = "id_token", Value = idToken });
                    context.Properties.StoreTokens(tokens);
                    context.Identity?.AddClaim(new System.Security.Claims.Claim("id_token", idToken));
                }
            }
            return Task.CompletedTask;
        };
        opts.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
        opts.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("GoogleAuthentication");
            logger.LogWarning(context.Failure, "Google OAuth callback failed.");
            context.HandleResponse();
            context.Response.Redirect("/Auth/AuthenticationError?message=" +
                Uri.EscapeDataString("Bạn đã hủy hoặc Google từ chối yêu cầu đăng nhập."));
            return Task.CompletedTask;
        };
    });
}

// (Optional) Chính sách phân quyền theo role
builder.Services.AddAuthorization(opts =>
{
    // ví dụ: chính sách chỉ cho Admin
    opts.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});
// Các dịch vụ khác
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.Name = ".TFlix.WebBrowser.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
app.MapHub<WebBrowser.Hubs.SupportChatHub>("/supportChatHub");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
