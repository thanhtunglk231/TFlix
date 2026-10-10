
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

// =====================================================
// FILE LOGGER
// =====================================================

builder.Logging.AddFileLogger(options =>
{
    options.LogDirectory = Path.Combine(
        builder.Environment.ContentRootPath,
        "Logs");

    options.FileNamePrefix = "server";
    options.MinLevel = LogLevel.Information;
    options.RetainDays = 30;
});

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        options.SerializerSettings.DateParseHandling =
            Newtonsoft.Json.DateParseHandling.None;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================================================
// REDIS - UPSTASH CLOUD
// =====================================================

var redisConfig = builder.Configuration.GetSection("Redis");

var redisHost = redisConfig["Host"]
    ?? throw new InvalidOperationException(
        "Redis Host is missing in configuration.");

var redisPassword = redisConfig["Password"]
    ?? throw new InvalidOperationException(
        "Redis Password is missing in configuration.");

var redisPort = redisConfig.GetValue<int>("Port", 6379);

var redisOptions = new ConfigurationOptions
{
    User = redisConfig["Username"] ?? "default",

    Password = redisPassword,

    Ssl = redisConfig.GetValue<bool>("Ssl", true),

    SslHost = redisHost,

    AbortOnConnectFail = redisConfig.GetValue<bool>(
        "AbortOnConnectFail", false),

    ConnectRetry = 5,

    ConnectTimeout = 15000,

    SyncTimeout = 15000,

    KeepAlive = 30
};

redisOptions.EndPoints.Add(redisHost, redisPort);

builder.Services.AddSingleton(redisOptions);
builder.Services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();

builder.Services.AddSingleton<
    IRedisCacheService,
    RedisCacheService>();

// =====================================================
// DATABASE & BUSINESS SERVICES
// =====================================================

builder.Services.AddScoped<ICBaseProvider, CBaseProvider>();
builder.Services.AddScoped<ICFilm, CFilm>();
builder.Services.AddScoped<ICAuth, CAuth>();

// =====================================================
// EMAIL & OTP
// =====================================================

builder.Services.Configure<Server.Models.SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthOtpService, AuthOtpService>();

// =====================================================
// MOVIE / SERIES / EPISODE
// =====================================================

builder.Services.AddScoped<ICMovie, CMovie>();
builder.Services.AddScoped<ICEpisode, CEpisode>();
builder.Services.AddScoped<ICSeason, CSeason>();
builder.Services.AddScoped<ICSeries, CSeries>();

builder.Services.AddScoped<ICVideoSoure, CVideoSoure>();

// =====================================================
// VIDEO TRANSCODING
// =====================================================

builder.Services.AddSingleton<
    IVideoTranscodingService,
    FfmpegVideoTranscodingService>();

builder.Services.AddSingleton<VideoUploadProgressTracker>();

// =====================================================
// CLOUD STORAGE
// =====================================================

builder.Services.AddSingleton<
    ICloudflareR2Service,
    CloudflareR2Service>();

builder.Services.AddSingleton<
    ISupabaseService,
    SupabaseService>();

// =====================================================
// MOVIE ASSETS & GENRES
// =====================================================

builder.Services.AddScoped<ICEpisodeAssets, CEpisodeAssets>();
builder.Services.AddScoped<ICMovieAsset, CMovieAsset>();
builder.Services.AddScoped<ICGenres, CGenres>();
builder.Services.AddScoped<ICMovieGenre, CMovieGenre>();
builder.Services.AddScoped<ICSeriesGenres, CSeriesGenres>();

// =====================================================
// ADMIN SERVICES
// =====================================================

builder.Services.AddScoped<ICPermission, CPermission>();
builder.Services.AddScoped<ICAdminAccount, CAdminAccount>();
builder.Services.AddScoped<IPreView, PreView>();
builder.Services.AddScoped<ICHome, CHome>();
builder.Services.AddScoped<ICNews, CNews>();
builder.Services.AddScoped<ICComment, CComment>();
builder.Services.AddScoped<ICFavorite, CFavorite>();
builder.Services.AddScoped<ICRating, CRating>();


// =====================================================
// SUBSCRIPTION & PAYMENT - PAYOS
// =====================================================

builder.Services.AddScoped<
    ICSubscriptionPlan,
    CSubscriptionPlan>();

builder.Services.AddScoped<ICPayment, CPayment>();

builder.Services.AddHttpClient<
    IPayOsService,
    PayOsService>();

// =====================================================
// GEMINI AI CHATBOT
// =====================================================

builder.Services.AddHttpClient("GeminiClient");

builder.Services.Configure<CoreLib.Config.GeminiOptions>(
    builder.Configuration.GetSection(
        CoreLib.Config.GeminiOptions.SectionName));

builder.Services.Configure<CoreLib.Config.ChatbotOptions>(
    builder.Configuration.GetSection(
        CoreLib.Config.ChatbotOptions.SectionName));

builder.Services.AddSingleton<
    Server.Services.Gemini.IGeminiClient,
    Server.Services.Gemini.GeminiClient>();

builder.Services.AddScoped<
    Server.Services.Chat.IChatbotService,
    Server.Services.Chat.ChatbotService>();

// =====================================================
// JWT AUTHENTICATION
// =====================================================

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtSettings["SecretKey"]!))
        };
});

// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();

var requestLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("ControllerRequest");

// =====================================================
// GLOBAL EXCEPTION LOGGING
// =====================================================

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");

        logger.LogError(
            ex,
            "Unhandled exception in Server");

        throw;
    }
});

// =====================================================
// SWAGGER
// =====================================================

app.UseSwagger();
app.UseSwaggerUI();

// =====================================================
// ROUTING
// =====================================================

app.UseRouting();

// =====================================================
// CONTROLLER REQUEST LOGGING
// =====================================================

app.Use(async (context, next) =>
{
    var action = context.GetEndpoint()
        ?.Metadata
        .GetMetadata<ControllerActionDescriptor>();

    if (action == null)
    {
        await next();
        return;
    }

    var stopwatch = Stopwatch.StartNew();

    var statusCode =
        StatusCodes.Status500InternalServerError;

    try
    {
        await next();
        statusCode = context.Response.StatusCode;
    }
    finally
    {
        stopwatch.Stop();

        requestLogger.LogInformation(
            "Controller {Controller}.{Action} handled " +
            "{Method} {Path} with status {StatusCode} " +
            "in {ElapsedMilliseconds} ms",
            action.ControllerName,
            action.ActionName,
            context.Request.Method,
            context.Request.Path,
            statusCode,
            stopwatch.ElapsedMilliseconds);
    }
});

// =====================================================
// AUTHENTICATION & AUTHORIZATION
// =====================================================

app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// REDIS HEALTH CHECK
// =====================================================

// Chỉ bật endpoint này trong môi trường Development.
// Không công khai thông tin Redis trên production.

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/health/redis",
        async (IRedisConnectionProvider redis) =>
    {
        try
        {
            var database = await redis.GetDatabaseAsync();
            if (database is null)
                return Results.Problem("Redis connection failed", statusCode: 503);

            var latency = await database.PingAsync();

            return Results.Ok(new
            {
                Connected = true,
                LatencyMs = latency.TotalMilliseconds,
                Message = "Redis connected successfully"
            });
        }
        catch
        {
            return Results.Problem(
                "Redis connection failed",
                statusCode: 503);
        }
    });
}

// =====================================================
// MAP CONTROLLERS
// =====================================================

app.MapControllers();

// =====================================================
// RUN APPLICATION
// =====================================================

app.Run();
