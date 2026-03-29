using CoreLib.Dtos.SeriesGenre;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements.Admin.Series
{
    public class CSeriesGenres : ICSeriesGenres
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CSeriesGenres(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> sp_get_all()
        {
            try
            {
                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_get_all",
                    new IDbDataParameter[] { o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_get_by_pk(decimal seriesId, decimal genreId)
        {
            try
            {
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = seriesId,
                    Direction = ParameterDirection.Input
                };
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal)
                {
                    Value = genreId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_get_by_pk",
                    new IDbDataParameter[] { p_series_id, p_genre_id, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_get_by_series(decimal seriesId)
        {
            try
            {
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = seriesId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_get_by_series",
                    new IDbDataParameter[] { p_series_id, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_get_by_genre(decimal genreId)
        {
            try
            {
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal)
                {
                    Value = genreId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_get_by_genre",
                    new IDbDataParameter[] { p_genre_id, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_add(AddSeriesGenreDto addSeriesGenreDto)
        {
            try
            {
                Console.WriteLine("=== CALL sp_series_genre_add ===");
                Console.WriteLine($"SeriesId: {addSeriesGenreDto.SeriesId}");
                Console.WriteLine($"GenreId: {addSeriesGenreDto.GenreId}");

                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = addSeriesGenreDto.SeriesId,
                    Direction = ParameterDirection.Input
                };

                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal)
                {
                    Value = addSeriesGenreDto.GenreId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_add",
                    new IDbDataParameter[] { p_series_id, p_genre_id, o_code, o_message },
                    _connectionString
                );

                Console.WriteLine("=== SP RESULT ===");
                Console.WriteLine($"o_code: {o_code.Value}");
                Console.WriteLine($"o_message: {o_message.Value}");

                if (ds != null && ds.Tables.Count > 0)
                {
                    Console.WriteLine($"Tables count: {ds.Tables.Count}");
                    Console.WriteLine($"Rows count: {ds.Tables[0].Rows.Count}");
                }
                else
                {
                    Console.WriteLine("Dataset NULL hoặc không có bảng");
                }

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR ===");
                Console.WriteLine(ex.ToString());

                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_update(UpdateSeriesGenreDto updateSeriesGenreDto)
        {
            try
            {
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = updateSeriesGenreDto.SeriesId,
                    Direction = ParameterDirection.Input
                };
                var p_old_genre_id = new SqlParameter("@p_old_genre_id", SqlDbType.Decimal)
                {
                    Value = updateSeriesGenreDto.OldGenreId,
                    Direction = ParameterDirection.Input
                };
                var p_new_genre_id = new SqlParameter("@p_new_genre_id", SqlDbType.Decimal)
                {
                    Value = updateSeriesGenreDto.NewGenreId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_update",
                    new IDbDataParameter[] { p_series_id, p_old_genre_id, p_new_genre_id, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }

        public async Task<CResponseMessage> sp_delete(DeleteSeriesGenreDto deleteSeriesGenreDto)
        {
            try
            {
                var p_series_id = new SqlParameter("@p_series_id", SqlDbType.Decimal)
                {
                    Value = deleteSeriesGenreDto.SeriesId,
                    Direction = ParameterDirection.Input
                };
                var p_genre_id = new SqlParameter("@p_genre_id", SqlDbType.Decimal)
                {
                    Value = deleteSeriesGenreDto.GenreId,
                    Direction = ParameterDirection.Input
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_series_genre_delete",
                    new IDbDataParameter[] { p_series_id, p_genre_id, o_code, o_message },
                    _connectionString
                );

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }
        }
    }
}