using CoreLib.Dtos.Rating;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataServiceLib.Implements;

public class CRating : ICRating
{
    private readonly ICBaseProvider _baseProvider;
    private readonly string _connectionString;

    public CRating(ICBaseProvider baseProvider, IConfiguration configuration)
    {
        _baseProvider = baseProvider;
        _connectionString = configuration.GetConnectionString("SqlServer") ?? string.Empty;
    }

    public Task<CResponseMessage> GetMovieRatingAsync(long movieId, long? userId = null)
    {
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

            var parameters = new IDbDataParameter[]
            {
                new SqlParameter("@p_movie_id", SqlDbType.BigInt) { Value = movieId },
                new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = userId.HasValue && userId.Value > 0 ? (object)userId.Value : DBNull.Value },
                code,
                message
            };

            var dataSet = _baseProvider.GetDatasetFromSP("usp_MovieRating_Get", parameters, _connectionString);
            var result = new MovieRatingDto
            {
                MovieId = movieId,
                AverageRating = 0,
                RatingCount = 0,
                UserRating = null
            };

            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                result.AverageRating = row["AverageRating"] != DBNull.Value ? Convert.ToDecimal(row["AverageRating"]) : 0;
                result.RatingCount = row["RatingCount"] != DBNull.Value ? Convert.ToInt32(row["RatingCount"]) : 0;
                result.UserRating = row["UserRating"] != DBNull.Value ? Convert.ToInt32(row["UserRating"]) : null;
            }

            var responseCode = code.Value?.ToString() ?? "200";
            return Task.FromResult(new CResponseMessage
            {
                Success = responseCode == "200",
                code = responseCode,
                message = message.Value?.ToString() ?? "Thành công",
                Data = result
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Lỗi khi lấy thông tin đánh giá: " + ex.Message
            });
        }
    }

    public async Task<CResponseMessage> SetMovieRatingAsync(SetMovieRatingRequest request)
    {
        try
        {
            var code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

            var parameters = new IDbDataParameter[]
            {
                new SqlParameter("@p_user_id", SqlDbType.BigInt) { Value = request.UserId },
                new SqlParameter("@p_movie_id", SqlDbType.BigInt) { Value = request.MovieId },
                new SqlParameter("@p_rating_val", SqlDbType.Int) { Value = request.RatingVal },
                code,
                message
            };

            _baseProvider.ExecuteSP("usp_MovieRating_Set", parameters, _connectionString);

            var responseCode = code.Value?.ToString() ?? "500";
            if (responseCode == "200")
            {
                // Lấy lại thống kê rating cập nhật mới nhất
                var updated = await GetMovieRatingAsync(request.MovieId, request.UserId);
                return new CResponseMessage
                {
                    Success = true,
                    code = "200",
                    message = message.Value?.ToString() ?? "Đã lưu đánh giá thành công.",
                    Data = updated.Data
                };
            }

            return new CResponseMessage
            {
                Success = false,
                code = responseCode,
                message = message.Value?.ToString() ?? "Không thể lưu đánh giá."
            };
        }
        catch (Exception ex)
        {
            return new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Lỗi khi lưu đánh giá: " + ex.Message
            };
        }
    }
}
