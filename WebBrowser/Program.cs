using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ? ??ng ký HttpClientFactory (fix l?i IHttpClientFactory)
builder.Services.AddHttpClient();

// ? DI cho services
builder.Services.AddScoped<WebBrowser.Services.HttpSevice.Interfaces.IHttpService,
                           WebBrowser.Services.HttpSevice.Implements.HttpService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IAuthService,
                           WebBrowser.Services.Implements.AuthService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISeriesService,
                           WebBrowser.Services.Implements.SeriesService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.ISesonService,
                           WebBrowser.Services.Implements.SesonService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IMovieService,
                           WebBrowser.Services.Implements.MovieService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IEpisode,
                           WebBrowser.Services.Implements.Episode>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IVideoSoureService,
                           WebBrowser.Services.Implements.VideoSoureService>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IEpisodeAsset,
                           WebBrowser.Services.Implements.EpisodeAsset>();
builder.Services.AddScoped<WebBrowser.Services.Interfaces.IMovieAssetService,
                           WebBrowser.Services.Implements.MovieAssetService>();

builder.Services.AddScoped<WebBrowser.Services.Interfaces.IGenresService,
                           WebBrowser.Services.Implements.GenresService>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ?? NH? b?t Session trong pipeline
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
