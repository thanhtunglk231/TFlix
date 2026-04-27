using Amazon.S3;
using Amazon.S3.Model;
using CoreLib.Models;
using DataServiceLib.Implements;
using DataServiceLib.Implements.Admin;
using DataServiceLib.Implements.Admin.Episodes;
using DataServiceLib.Implements.Admin.Movies;
using DataServiceLib.Implements.Admin.Series;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.CloudFlareServices;
using System.Data.SqlClient;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.DateParseHandling = Newtonsoft.Json.DateParseHandling.None;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICBaseProvider, CBaseProvider>();
builder.Services.AddScoped<ICFilm, CFilm>();
builder.Services.AddScoped<ICAuth, CAuth>();
builder.Services.AddScoped<ICMovie, CMovie>();
builder.Services.AddScoped<ICEpisode, CEpisode>();
builder.Services.AddScoped<ICSeason, CSeason>();
builder.Services.AddScoped<ICSeries, CSeries>();
builder.Services.AddScoped<ICVideoSoure, CVideoSoure>();
builder.Services.AddSingleton<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<ICEpisodeAssets, CEpisodeAssets>();
builder.Services.AddScoped<ICMovieAsset, CMovieAsset>();
builder.Services.AddScoped<ICGenres, CGenres>();
builder.Services.AddScoped<ICMovieGenre, CMovieGenre>();
builder.Services.AddScoped<ICSeriesGenres, CSeriesGenres>();
builder.Services.AddScoped<IPreView, PreView>();
builder.Services.AddScoped<ICHome, CHome>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var config = builder.Configuration;
var connStr = config.GetConnectionString("SqlServer");

// test kết nối
using (SqlConnection conn = new SqlConnection(connStr))
{
    try
    {
        conn.Open();
        Console.WriteLine("✅ SQL Server connected successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Connection failed:");
        Console.WriteLine(ex.Message);
    }
}
builder.Services.Configure<CloudflareR2Options>(
    builder.Configuration.GetSection("CloudflareR2"));

builder.Services.AddSingleton<IR2Service, R2Service>();
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

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    try
    {
        var r2Options = scope.ServiceProvider
            .GetRequiredService<IOptions<CloudflareR2Options>>()
            .Value;

        var endpoint = $"https://{r2Options.AccountId}.r2.cloudflarestorage.com";

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        using var s3Client = new AmazonS3Client(
            r2Options.AccessKey,
            r2Options.SecretKey,
            s3Config);

        var key = "test-connection.txt";
        var content = $"hello r2 - {DateTime.UtcNow:O}";
        var bytes = Encoding.UTF8.GetBytes(content);

        using var stream = new MemoryStream(bytes);

        var putRequest = new PutObjectRequest
        {
            BucketName = r2Options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "text/plain",
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest);

        Console.WriteLine("✅ R2 upload success");
        Console.WriteLine($"{r2Options.PublicBaseUrl.TrimEnd('/')}/{key}");
    }
    catch (AmazonS3Exception ex)
    {
        Console.WriteLine("❌ R2 S3 error");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"StatusCode: {ex.StatusCode}");
        Console.WriteLine($"ErrorCode: {ex.ErrorCode}");
        Console.WriteLine($"RequestId: {ex.RequestId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ R2 connection failed");
        Console.WriteLine(ex.Message);
    }
}
app.Run();