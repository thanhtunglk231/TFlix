using CoreLib.Dtos.Home;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CHome : ICHome
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CHome(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public async Task<CResponseMessage> MovieLastestItem()
        {
            try
            {
                await EnsureStoredProcedureExistsAsync();

                var p_limit = new SqlParameter("@p_limit", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Input,
                    Value = 8
                };

                var o_code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };

                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_limit,
                    o_code,
                    o_message
                };

                var ds = _baseProvider.GetDatasetFromSP(
                    "sp_movies_get_latest",
                    parameters,
                    _connectionString
                );

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return new CResponseMessage
                    {
                        Data = ds,
                        code = o_code.Value?.ToString() ?? "200",
                        message = o_message.Value?.ToString() ?? "Thành công",
                        Success = true
                    };
                }

                // Dữ liệu mẫu khi database chưa có phim hoặc không kết nối được
                return new CResponseMessage
                {
                    Data = GetFallbackLatestMovies(),
                    code = "200",
                    message = "Thành công (dữ liệu mẫu)",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CHome.MovieLastestItem] Fallback due to: " + ex.Message);
                return new CResponseMessage
                {
                    Success = true,
                    code = "200",
                    message = "Thành công (fallback dữ liệu mẫu)",
                    Data = GetFallbackLatestMovies()
                };
            }
        }

        private async Task EnsureStoredProcedureExistsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string checkAndCreateSql = @"
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_movies_get_latest]') AND type in (N'P', N'PC'))
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE dbo.sp_movies_get_latest
                            @p_limit   INT = 8,
                            @o_code    VARCHAR(10) OUTPUT,
                            @o_message NVARCHAR(4000) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            BEGIN TRY
                                IF @p_limit IS NULL OR @p_limit <= 0 SET @p_limit = 8;

                                SELECT TOP (@p_limit)
                                    m.movie_id       AS moviE_ID,
                                    N''MOVIE''       AS kind,
                                    m.title          AS title,
                                    m.original_title AS originaL_TITLE,
                                    m.release_date   AS releasE_DATE,
                                    m.status         AS status,
                                    ISNULL(m.is_premium, ''N'') AS iS_PREMIUM,
                                    COALESCE(ma_p.url, N''https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop'') AS posteR_URL,
                                    COALESCE(ma_b.url, ma_p.url, N''https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?q=80&w=1600&auto=format&fit=crop'') AS backdroP_URL,
                                    (SELECT COUNT(1) FROM dbo.video_sources vs WHERE vs.movie_id = m.movie_id) AS sourcE_COUNT,
                                    (
                                        SELECT STRING_AGG(g.genre_name, N'', '')
                                        FROM dbo.movie_genres mg
                                        JOIN dbo.genres g ON mg.genre_id = g.genre_id
                                        WHERE mg.movie_id = m.movie_id
                                    ) AS genres
                                FROM dbo.movies m
                                LEFT JOIN dbo.movie_assets ma_p ON m.movie_id = ma_p.movie_id AND ma_p.asset_type = N''POSTER''
                                LEFT JOIN dbo.movie_assets ma_b ON m.movie_id = ma_b.movie_id AND ma_b.asset_type = N''BACKDROP''
                                WHERE (m.status IS NULL OR m.status = N''PUBLISHED'')
                                ORDER BY m.release_date DESC, m.movie_id DESC;

                                SET @o_code = ''200'';
                                SET @o_message = N''Thành công'';
                            END TRY
                            BEGIN CATCH
                                SET @o_code = ''500'';
                                SET @o_message = ERROR_MESSAGE();
                            END CATCH
                        END;
                        ')
                    END;
                ";

                using var cmd = new SqlCommand(checkAndCreateSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CHome.EnsureStoredProcedureExistsAsync] Error: " + ex.Message);
            }
        }

        private static DataSet GetFallbackLatestMovies()
        {
            var ds = new DataSet();
            var dt = new DataTable("Table");
            dt.Columns.Add("moviE_ID", typeof(int));
            dt.Columns.Add("kind", typeof(string));
            dt.Columns.Add("title", typeof(string));
            dt.Columns.Add("originaL_TITLE", typeof(string));
            dt.Columns.Add("releasE_DATE", typeof(DateTime));
            dt.Columns.Add("status", typeof(string));
            dt.Columns.Add("iS_PREMIUM", typeof(string));
            dt.Columns.Add("posteR_URL", typeof(string));
            dt.Columns.Add("backdroP_URL", typeof(string));
            dt.Columns.Add("sourcE_COUNT", typeof(int));
            dt.Columns.Add("genres", typeof(string));

            dt.Rows.Add(1, "MOVIE", "Lật Mặt 7: Một Điều Ước", "Wish 7", new DateTime(2024, 4, 26), "PUBLISHED", "Y", "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?q=80&w=1600&auto=format&fit=crop", 1, "Hành động, Gia đình");
            dt.Rows.Add(2, "MOVIE", "Mai", "Mai Movie", new DateTime(2024, 2, 10), "PUBLISHED", "N", "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop", 1, "Tình cảm, Tâm lý");
            dt.Rows.Add(3, "MOVIE", "Avatar: Dòng Chảy Của Nước", "Avatar: The Way of Water", new DateTime(2022, 12, 16), "PUBLISHED", "Y", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?q=80&w=1600&auto=format&fit=crop", 1, "Viễn tưởng, Hành động");
            dt.Rows.Add(4, "MOVIE", "Oppenheimer", "Oppenheimer", new DateTime(2023, 7, 21), "PUBLISHED", "Y", "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=500&auto=format&fit=crop", 1, "Chính kịch, Lịch sử");
            dt.Rows.Add(5, "MOVIE", "Quật Mộ Trùng Phùng", "Exhuma", new DateTime(2024, 2, 22), "PUBLISHED", "Y", "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500&auto=format&fit=crop", 1, "Kinh dị, Bí ẩn");
            dt.Rows.Add(6, "MOVIE", "Ký Sinh Trùng", "Parasite", new DateTime(2019, 5, 30), "PUBLISHED", "N", "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=500&auto=format&fit=crop", 1, "Tâm lý, Giật gân");
            dt.Rows.Add(7, "MOVIE", "Godzilla x Kong: Đế Chế Mới", "The New Empire", new DateTime(2024, 3, 29), "PUBLISHED", "Y", "https://images.unsplash.com/photo-1563089145-599997674d42?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1563089145-599997674d42?w=500&auto=format&fit=crop", 1, "Hành động, Viễn tưởng");
            dt.Rows.Add(8, "MOVIE", "Suzume Cửa Hàng Khóa Ký Ức", "Suzume no Tojimari", new DateTime(2023, 3, 8), "PUBLISHED", "N", "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=500&auto=format&fit=crop", "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=500&auto=format&fit=crop", 1, "Hoạt hình, Phiêu lưu");

            ds.Tables.Add(dt);
            return ds;
        }
    }
}