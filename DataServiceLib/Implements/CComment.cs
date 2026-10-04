using CoreLib.Dtos.Comment;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CComment : ICComment
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string? _connectionString;

        public CComment(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        private async Task EnsureStoredProceduresAndTableAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString)) return;

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string ddl = @"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'comments')
                BEGIN
                    CREATE TABLE dbo.comments (
                        comment_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
                        movie_id     BIGINT NULL,
                        episode_id   BIGINT NULL,
                        user_id      BIGINT NOT NULL,
                        user_name    NVARCHAR(255) NULL,
                        avatar_url   NVARCHAR(500) NULL,
                        content      NVARCHAR(MAX) NOT NULL,
                        status       NVARCHAR(50) DEFAULT 'ACTIVE',
                        created_at   DATETIME DEFAULT GETDATE()
                    );

                    CREATE INDEX IX_comments_movie ON dbo.comments(movie_id);
                    CREATE INDEX IX_comments_episode ON dbo.comments(episode_id);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_Comment_GetByContent]') AND type in (N'P', N'PC'))
                BEGIN
                    EXEC('
                    CREATE PROCEDURE dbo.usp_Comment_GetByContent
                        @p_movie_id   BIGINT = NULL,
                        @p_episode_id BIGINT = NULL,
                        @o_code       NVARCHAR(10) OUTPUT,
                        @o_message    NVARCHAR(4000) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        BEGIN TRY
                            SELECT 
                                c.comment_id   AS CommentId,
                                c.movie_id     AS MovieId,
                                c.episode_id   AS EpisodeId,
                                c.user_id      AS UserId,
                                c.user_name    AS UserName,
                                c.avatar_url   AS UserAvatar,
                                c.content      AS Content,
                                CAST(c.created_at AS DATETIME2) AS CreatedAt,
                                c.status       AS Status
                            FROM dbo.comments c
                            WHERE (c.status IS NULL OR c.status IN (''ACTIVE'', ''APPROVED''))
                              AND (
                                    (@p_episode_id IS NOT NULL AND c.episode_id = @p_episode_id)
                                    OR
                                    (@p_episode_id IS NULL AND @p_movie_id IS NOT NULL AND c.movie_id = @p_movie_id AND (c.episode_id IS NULL OR c.episode_id = 0))
                                  )
                            ORDER BY c.created_at DESC;

                            SET @o_code = N''200'';
                            SET @o_message = N''Thành công'';
                        END TRY
                        BEGIN CATCH
                            SET @o_code = N''500'';
                            SET @o_message = ERROR_MESSAGE();
                        END CATCH
                    END;
                    ')
                END

                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_Comment_Add]') AND type in (N'P', N'PC'))
                BEGIN
                    EXEC('
                    CREATE PROCEDURE dbo.usp_Comment_Add
                        @p_movie_id   BIGINT = NULL,
                        @p_episode_id BIGINT = NULL,
                        @p_user_id    BIGINT,
                        @p_user_name  NVARCHAR(255) = NULL,
                        @p_avatar_url NVARCHAR(500) = NULL,
                        @p_content    NVARCHAR(MAX),
                        @o_comment_id BIGINT OUTPUT,
                        @o_code       NVARCHAR(10) OUTPUT,
                        @o_message    NVARCHAR(4000) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        BEGIN TRY
                            IF @p_content IS NULL OR LTRIM(RTRIM(@p_content)) = ''''
                            BEGIN
                                SET @o_code = N''400'';
                                SET @o_message = N''Nội dung bình luận không được để trống'';
                                RETURN;
                            END;

                            INSERT INTO dbo.comments (
                                movie_id, episode_id, user_id, user_name, avatar_url, content, status, created_at
                            )
                            VALUES (
                                @p_movie_id, @p_episode_id, @p_user_id, @p_user_name, @p_avatar_url, @p_content, ''APPROVED'', GETDATE()
                            );

                            SET @o_comment_id = SCOPE_IDENTITY();
                            SET @o_code = N''200'';
                            SET @o_message = N''Thêm bình luận thành công'';
                        END TRY
                        BEGIN CATCH
                            SET @o_comment_id = 0;
                            SET @o_code = N''500'';
                            SET @o_message = ERROR_MESSAGE();
                        END CATCH
                    END;
                    ')
                END
                ";

                using var cmd = new SqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CComment.EnsureStoredProceduresAndTableAsync] Warning: " + ex.Message);
            }
        }

        public async Task<CResponseMessage> GetCommentsByContentAsync(long? movieId, long? episodeId)
        {
            try
            {
                await EnsureStoredProceduresAndTableAsync();

                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Input,
                    Value = movieId.HasValue ? movieId.Value : DBNull.Value
                };
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Input,
                    Value = episodeId.HasValue ? episodeId.Value : DBNull.Value
                };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[] { p_movie_id, p_episode_id, o_code, o_message };
                var dataset = _baseProvider.GetDatasetFromSP("usp_Comment_GetByContent", parameters, _connectionString ?? "");

                var list = new List<CommentDto>();
                if (dataset != null && dataset.Tables.Count > 0)
                {
                    foreach (DataRow row in dataset.Tables[0].Rows)
                    {
                        var createdAt = ConvertCreatedAt(row["CreatedAt"]);
                        list.Add(new CommentDto
                        {
                            CommentId = Convert.ToInt64(row["CommentId"]),
                            MovieId = row["MovieId"] != DBNull.Value ? Convert.ToInt64(row["MovieId"]) : null,
                            EpisodeId = row["EpisodeId"] != DBNull.Value ? Convert.ToInt64(row["EpisodeId"]) : null,
                            UserId = row["UserId"] != DBNull.Value ? Convert.ToInt64(row["UserId"]) : 0,
                            UserName = row["UserName"] != DBNull.Value ? row["UserName"].ToString() : "Người dùng TFlix",
                            UserAvatar = row["UserAvatar"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["UserAvatar"].ToString())
                                ? row["UserAvatar"].ToString()
                                : "https://i.pravatar.cc/40?u=" + (row["UserId"] != DBNull.Value ? row["UserId"].ToString() : "guest"),
                            Content = row["Content"]?.ToString() ?? "",
                            CreatedAt = createdAt,
                            FormattedTime = createdAt.HasValue ? GetTimeAgo(createdAt.Value) : "Vừa xong"
                        });
                    }
                }

                var responseCode = o_code.Value?.ToString() ?? "500";
                return new CResponseMessage
                {
                    Success = responseCode == "200",
                    code = responseCode,
                    message = o_message.Value?.ToString() ?? "Thành công",
                    Data = list
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi lấy bình luận: " + ex.Message,
                    Data = new List<CommentDto>()
                };
            }
        }

        public async Task<CResponseMessage> AddCommentAsync(CreateCommentDto request)
        {
            try
            {
                await EnsureStoredProceduresAndTableAsync();

                var p_movie_id = new SqlParameter("@p_movie_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Input,
                    Value = request.MovieId.HasValue ? request.MovieId.Value : DBNull.Value
                };
                var p_episode_id = new SqlParameter("@p_episode_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Input,
                    Value = request.EpisodeId.HasValue ? request.EpisodeId.Value : DBNull.Value
                };
                var p_user_id = new SqlParameter("@p_user_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Input,
                    Value = request.UserId
                };
                var p_user_name = new SqlParameter("@p_user_name", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Input,
                    Value = (object?)request.UserName ?? DBNull.Value
                };
                var p_avatar_url = new SqlParameter("@p_avatar_url", SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Input,
                    Value = (object?)request.UserAvatar ?? DBNull.Value
                };
                var p_content = new SqlParameter("@p_content", SqlDbType.NVarChar, -1)
                {
                    Direction = ParameterDirection.Input,
                    Value = request.Content
                };

                var o_comment_id = new SqlParameter("@o_comment_id", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                var o_code = new SqlParameter("@o_code", SqlDbType.NVarChar, 10)
                {
                    Direction = ParameterDirection.Output
                };
                var o_message = new SqlParameter("@o_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_movie_id, p_episode_id, p_user_id, p_user_name, p_avatar_url, p_content,
                    o_comment_id, o_code, o_message
                };

                var executed = _baseProvider.ExecuteSP("usp_Comment_Add", parameters, _connectionString ?? "");

                long newId = 0;
                if (o_comment_id.Value != null && o_comment_id.Value != DBNull.Value)
                {
                    newId = Convert.ToInt64(o_comment_id.Value);
                }

                var responseCode = o_code.Value?.ToString() ?? "500";
                if (!executed || responseCode != "200" || newId <= 0)
                {
                    return new CResponseMessage
                    {
                        Success = false,
                        code = responseCode,
                        message = o_message.Value?.ToString() ?? "Không thể lưu bình luận.",
                        Data = null
                    };
                }

                var createdItem = new CommentDto
                {
                    CommentId = newId,
                    MovieId = request.MovieId,
                    EpisodeId = request.EpisodeId,
                    UserId = request.UserId,
                    UserName = string.IsNullOrWhiteSpace(request.UserName) ? "Người dùng TFlix" : request.UserName,
                    UserAvatar = !string.IsNullOrWhiteSpace(request.UserAvatar) ? request.UserAvatar : $"https://i.pravatar.cc/40?u={request.UserId}",
                    Content = request.Content,
                    CreatedAt = DateTime.Now,
                    FormattedTime = "Vừa xong"
                };

                return new CResponseMessage
                {
                    Success = true,
                    code = responseCode,
                    message = o_message.Value?.ToString() ?? "Bình luận thành công",
                    Data = createdItem
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi thêm bình luận: " + ex.Message
                };
            }
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} ngày trước";
            return dt.ToString("dd/MM/yyyy");
        }

        private static DateTime? ConvertCreatedAt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is DateTimeOffset offset)
                return offset.LocalDateTime;

            if (value is DateTime dateTime)
                return dateTime;

            return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }
    }
}
