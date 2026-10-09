using CommonLib.Logging;
using DataServiceLib.Implements;
using DataServiceLib.Implements.Admin;
using DataServiceLib.Implements.Admin.CMS;
using DataServiceLib.Implements.Admin.Episodes;
using DataServiceLib.Implements.Admin.Movies;
using DataServiceLib.Implements.Admin.Series;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Controllers;
using Server.Services;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình File Logger ghi lỗi ra file txt
builder.Logging.AddFileLogger(options =>
{
    options.LogDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
    options.FileNamePrefix = "server";
    options.MinLevel = LogLevel.Information;
    options.RetainDays = 30;
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.DateParseHandling = Newtonsoft.Json.DateParseHandling.None;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString = builder.Configuration["Redis"]
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConnectionString, true)));
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

builder.Services.AddScoped<ICBaseProvider, CBaseProvider>();
builder.Services.AddScoped<ICFilm, CFilm>();
builder.Services.AddScoped<ICAuth, CAuth>();
builder.Services.Configure<Server.Models.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthOtpService, AuthOtpService>();
builder.Services.AddScoped<ICMovie, CMovie>();
builder.Services.AddScoped<ICEpisode, CEpisode>();
builder.Services.AddScoped<ICSeason, CSeason>();
builder.Services.AddScoped<ICSeries, CSeries>();
builder.Services.AddScoped<ICVideoSoure, CVideoSoure>();
builder.Services.AddSingleton<IVideoTranscodingService, FfmpegVideoTranscodingService>();
builder.Services.AddSingleton<VideoUploadProgressTracker>();
builder.Services.AddSingleton<ICloudflareR2Service, CloudflareR2Service>();
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<ICEpisodeAssets, CEpisodeAssets>();
builder.Services.AddScoped<ICMovieAsset, CMovieAsset>();
builder.Services.AddScoped<ICGenres, CGenres>();
builder.Services.AddScoped<ICMovieGenre, CMovieGenre>();
builder.Services.AddScoped<ICSeriesGenres, CSeriesGenres>();
builder.Services.AddScoped<ICPermission, CPermission>();
builder.Services.AddScoped<ICAdminAccount, CAdminAccount>();
builder.Services.AddScoped<IPreView, PreView>();
builder.Services.AddScoped<ICHome, CHome>();
builder.Services.AddScoped<ICNews, CNews>();
builder.Services.AddScoped<ICComment, CComment>();
builder.Services.AddScoped<ICFavorite, CFavorite>();
builder.Services.AddScoped<ICSubscriptionPlan, CSubscriptionPlan>();
builder.Services.AddScoped<ICPayment, CPayment>();
builder.Services.AddHttpClient<IPayOsService, PayOsService>();

builder.Services.AddHttpClient("GeminiClient");
builder.Services.Configure<CoreLib.Config.GeminiOptions>(builder.Configuration.GetSection(CoreLib.Config.GeminiOptions.SectionName));
builder.Services.Configure<CoreLib.Config.ChatbotOptions>(builder.Configuration.GetSection(CoreLib.Config.ChatbotOptions.SectionName));
builder.Services.AddSingleton<Server.Services.Gemini.IGeminiClient, Server.Services.Gemini.GeminiClient>();
builder.Services.AddScoped<Server.Services.Chat.IChatbotService, Server.Services.Chat.ChatbotService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
    };
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
        logger.LogError(ex, "Unhandled exception in Server");
        throw;
    }
});

app.UseSwagger();
app.UseSwaggerUI();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
