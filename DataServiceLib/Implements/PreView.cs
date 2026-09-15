using CoreLib.Dtos.Preview;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class PreView : IPreView
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public PreView(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }


    public CResponseMessage Get_All_Series(int seriesId)
    {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_series_preview", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // IN
                cmd.Parameters.Add(new SqlParameter("@p_series_id", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = seriesId
                });

                // Output parameters for code/message
                cmd.Parameters.Add(new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                });

                cmd.Parameters.Add(new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                });

                var ds = new DataSet();
                using (var da = new SqlDataAdapter(cmd))
                {
                    conn.Open();
                    da.Fill(ds); // first resultset is the SP result
                }

                var code = cmd.Parameters["@o_code"].Value?.ToString() ?? "400";
                var msg = cmd.Parameters["@o_message"].Value?.ToString() ?? "Không lấy được phản hồi";

                return new CResponseMessage
                {
                    Data = ds,
                    code = code,
                    message = msg,
                    Success = code == "200"
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
    public CResponseMessage get_all(int movieId)
        {
            try
            {
                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.Int) { Direction = ParameterDirection.Input, Value = movieId };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, o_code, o_message };

                var dataset = _baseProvider.GetDatasetFromSP("sp_movie_preview", parameters, _connectionString);
                return new CResponseMessage
                {
                    Data = dataset,
                    code = o_code.Value?.ToString() ?? "400",
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

        private void EnsureSpGetContentByIdExists()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                string sql = @"
                    CREATE OR ALTER PROCEDURE dbo.SP_GET_CONTENT_BY_ID
                        @p_id BIGINT = NULL,
                        @p_code NVARCHAR(50) = NULL,
                        @p_kind NVARCHAR(50) = N'movie',
                        @o_code NVARCHAR(10) OUTPUT,
                        @o_message NVARCHAR(4000) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        BEGIN TRY
                            IF @p_id IS NULL AND @p_code IS NOT NULL AND ISNUMERIC(@p_code) = 1
                            BEGIN
                                SET @p_id = CAST(@p_code AS BIGINT);
                            END;

                            SELECT TOP 1
                                m.movie_id AS MovieId,
                                m.movie_id AS id,
                                m.title AS title,
                                m.original_title AS OriginalTitle,
                                m.overview AS OverviewText,
                                m.release_date AS ReleaseOrAirDate,
                                m.duration_min AS DurationMin,
                                m.country_code AS CountryCode,
                                m.language_code AS LanguageCode,
                                m.status AS Status,
                                m.is_premium AS IsPremiumYN,
                                COALESCE(ma.url, N'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop') AS PrimaryPosterUrl,
                                vs.stream_url AS PrimaryStreamUrl,
                                (
                                    SELECT STRING_AGG(g.genre_name, N', ')
                                    FROM dbo.movie_genres mg
                                    JOIN dbo.genres g ON mg.genre_id = g.genre_id
                                    WHERE mg.movie_id = m.movie_id
                                ) AS genres
                            FROM dbo.movies m
                            LEFT JOIN dbo.movie_assets ma ON m.movie_id = ma.movie_id AND ma.asset_type = N'POSTER'
                            LEFT JOIN dbo.video_sources vs ON m.movie_id = vs.movie_id
                            WHERE (@p_id IS NOT NULL AND m.movie_id = @p_id)
                               OR (@p_id IS NULL AND 1=1);

                            SET @o_code = N'200';
                            SET @o_message = N'Thành công';
                        END TRY
                        BEGIN CATCH
                            SET @o_code = N'500';
                            SET @o_message = ERROR_MESSAGE();
                        END CATCH
                    END;
                ";
                using var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EnsureSpGetContentByIdExists] Error: " + ex.Message);
            }
        }

        public CResponseMessage GET_CONTENT_BY_ID(GETCONTENTByID movie)
        {
            try
            {
                EnsureSpGetContentByIdExists();

                Console.WriteLine("========== [SP_GET_CONTENT_BY_ID] CALL ==========");
                Console.WriteLine($"Input movie.id   = {movie?.id}");
                Console.WriteLine($"Input movie.kind = {movie?.kind}");
                Console.WriteLine("=================================================");

                var p_movie_id = new SqlParameter("@p_id", SqlDbType.BigInt) { Value = movie?.id ?? 0 };
                var p_code = new SqlParameter("@p_code", SqlDbType.NVarChar, 50) { Value = (object?)movie?.id.ToString() ?? DBNull.Value };
                var p_kind = new SqlParameter("@p_kind", SqlDbType.NVarChar, 50) { Value = (object?)movie?.kind ?? "movie" };

                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10) { Direction = ParameterDirection.Output };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                var parameters = new IDbDataParameter[] { p_movie_id, p_code, p_kind, o_code, o_message };

                // Gọi SP (SQL Server stored procedure should return a resultset directly)
                var dataset = _baseProvider.GetDatasetFromSP("dbo.SP_GET_CONTENT_BY_ID", parameters, _connectionString);

                // Debug dataset return
                if (dataset != null)
                {
                    Console.WriteLine($">>> Dataset Returned: Tables = {dataset.Tables.Count}");
                    for (int i = 0; i < dataset.Tables.Count; i++)
                    {
                        var tbl = dataset.Tables[i];
                        Console.WriteLine($"    Table[{i}] Name: {tbl.TableName} Rows: {tbl.Rows.Count} Cols: {tbl.Columns.Count}");
                    }
                }
                else
                {
                    Console.WriteLine(">>> Dataset = NULL");
                }

                // Đọc output của SP
                string outCode = o_code.Value?.ToString();
                string outMessage = o_message.Value?.ToString();

                Console.WriteLine(">>> SP Output:");
                Console.WriteLine($" - o_code    = {outCode}");
                Console.WriteLine($" - o_message = {outMessage}");
                Console.WriteLine("=================================================");

                return new CResponseMessage
                {
                    Data = dataset,
                    code = outCode ?? "200",
                    message = outMessage ?? "Thành công",
                    Success = (outCode == "200" || (dataset != null && dataset.Tables.Count > 0))
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("=========== [SP_GET_CONTENT_BY_ID] EXCEPTION ===========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================================================");

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
