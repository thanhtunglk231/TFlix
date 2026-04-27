using DataServiceLib.Implements;
using DataServiceLib.Implements.Admin;
using DataServiceLib.Implements.Admin.Episodes;
using DataServiceLib.Implements.Admin.Movies;
using DataServiceLib.Implements.Admin.Series;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

app.Run();