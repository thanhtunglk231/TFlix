using CoreLib.Dtos.Favorite;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements;

public class CFavorite : ICFavorite
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;

    public CFavorite(ICBaseProvider baseProvider, IConfiguration configuration)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer") ?? string.Empty;
    }

    public Task<CResponseMessage> GetMoviesAsync(long userId)
    {
        try
        {
            var parameters = CreateUserParameters(userId, out var code, out var message);
            var dataSet = _baseProvider.GetDatasetFromSP("usp_FavoriteMovie_GetByUser", parameters, _connectionString);
            var movies = new List<FavoriteMovieDto>();

            if (dataSet.Tables.Count > 0)
            {
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    movies.Add(new FavoriteMovieDto
                    {
                        MovieId = Convert.ToInt64(row["MovieId"]),
                        Title = row["Title"]?.ToString() ?? string.Empty,
                        OriginalTitle = row["OriginalTitle"] == DBNull.Value ? null : row["OriginalTitle"].ToString(),
                        Overview = row["Overview"] == DBNull.Value ? null : row["Overview"].ToString(),
                        ReleaseDate = row["ReleaseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ReleaseDate"]),
                        DurationMin = row["DurationMin"] == DBNull.Value ? null : Convert.ToInt32(row["DurationMin"]),
                        AgeRating = row["AgeRating"] == DBNull.Value ? null : row["AgeRating"].ToString(),
                        PosterUrl = row["PosterUrl"] == DBNull.Value ? null : row["PosterUrl"].ToString(),
                        AddedAt = (DateTimeOffset)row["AddedAt"]
                    });
                }
            }

            return Task.FromResult(Response(code, message, movies));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(ex));
        }
    }

    public Task<CResponseMessage> GetMovieIdsAsync(long userId)
    {
        try
        {
            var parameters = CreateUserParameters(userId, out var code, out var message);
            var dataSet = _baseProvider.GetDatasetFromSP("usp_FavoriteMovie_GetIds", parameters, _connectionString);
            var ids = dataSet.Tables.Count == 0
                ? new List<long>()
                : dataSet.Tables[0].AsEnumerable().Select(row => row.Field<long>("MovieId")).ToList();
            return Task.FromResult(Response(code, message, ids));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(ex));
        }
    }

    public Task<CResponseMessage> ToggleMovieAsync(FavoriteMovieRequest request)
    {
        try
        {
            var isFavorite = new SqlParameter("@o_is_favorite", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
            var parameters = new IDbDataParameter[]
            {
                new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = request.UserId },
                new SqlParameter("@p_movie_id", SqlDbType.BigInt) { Value = request.MovieId },
                isFavorite, code, message
            };
            _baseProvider.ExecuteSP("usp_FavoriteMovie_Toggle", parameters, _connectionString);
            var result = new FavoriteToggleResult
            {
                MovieId = request.MovieId,
                IsFavorite = isFavorite.Value != DBNull.Value && Convert.ToBoolean(isFavorite.Value)
            };
            return Task.FromResult(Response(code, message, result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(ex));
        }
    }

    private static IDbDataParameter[] CreateUserParameters(long userId, out SqlParameter code, out SqlParameter message)
    {
        code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
        message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };
        return new IDbDataParameter[]
        {
            new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = userId }, code, message
        };
    }

    private static CResponseMessage Response(SqlParameter code, SqlParameter message, object data)
    {
        var responseCode = code.Value?.ToString() ?? "500";
        return new CResponseMessage { Success = responseCode == "200", code = responseCode, message = message.Value?.ToString() ?? string.Empty, Data = data };
    }

    private static CResponseMessage Failure(Exception ex) => new() { Success = false, code = "500", message = "Lỗi xử lý phim yêu thích: " + ex.Message };
}
