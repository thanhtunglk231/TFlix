using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Config;
using CoreLib.Dtos.Chat;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Services.Gemini;

namespace Server.Services.Chat
{
    public class ChatbotService : IChatbotService
    {
        private readonly IGeminiClient _geminiClient;
        private readonly IRedisCacheService _cache;
        private readonly ICMovie _movieService;
        private readonly ICSeries _seriesService;
        private readonly ChatbotOptions _options;
        private readonly ILogger<ChatbotService> _logger;

        private class InternalMovieCandidate
        {
            public long Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? OriginalTitle { get; set; }
            public string? Overview { get; set; }
            public string? Genres { get; set; }
            public string? CountryCode { get; set; }
            public int? Year { get; set; }
            public decimal Rating { get; set; } = 8.5m;
            public string? PosterUrl { get; set; }
            public string Kind { get; set; } = "movie"; // "movie" hoặc "series"
        }

        public ChatbotService(
            IGeminiClient geminiClient,
            IRedisCacheService cache,
            ICMovie movieService,
            ICSeries seriesService,
            IOptions<ChatbotOptions> options,
            ILogger<ChatbotService> logger)
        {
            _geminiClient = geminiClient;
            _cache = cache;
            _movieService = movieService;
            _seriesService = seriesService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request, string userKey, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Nội dung tin nhắn không được để trống.");
            }

            var cleanMessage = request.Message.Trim();
            if (cleanMessage.Length > 500)
            {
                throw new ArgumentException("Tin nhắn vượt quá giới hạn tối đa (500 ký tự).");
            }

            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString("N")
                : request.ConversationId.Trim();

            // 1. Rate Limiting Check qua Redis
            await EnforceRateLimitAsync(userKey, cancellationToken);

            // 2. Lấy lịch sử hội thoại từ Redis
            var historyKey = $"tflix:chat:history:{userKey}:{conversationId}";
            var history = await _cache.GetAsync<List<ChatMessageDto>>(historyKey, cancellationToken)
                          ?? new List<ChatMessageDto>();

            // 3. Lấy Catalog phim thực tế của TFlix từ Database (có Cache)
            var allMovies = await GetTFlixCatalogAsync(cancellationToken);

            // 4. Lọc phim ứng viên (Candidate Retrieval) phù hợp với truy vấn
            var candidateMovies = SelectCandidates(allMovies, cleanMessage, history);

            // 5. Chuẩn bị System Prompt & Candidate Context
            var systemPrompt = BuildSystemPrompt(candidateMovies);

            // 6. Gọi Gemini API để sinh câu trả lời
            string? aiReplyText = null;
            if (_geminiClient.IsConfigured)
            {
                aiReplyText = await _geminiClient.GenerateContentAsync(
                    systemPrompt,
                    history.TakeLast(_options.MaxHistoryMessages),
                    cleanMessage,
                    cancellationToken);
            }

            List<ChatMovieCardDto> matchedCards;

            if (!string.IsNullOrWhiteSpace(aiReplyText))
            {
                // Trích xuất Movie Suggestions từ phản hồi của Gemini
                (aiReplyText, matchedCards) = ExtractMovieSuggestions(aiReplyText, candidateMovies);
            }
            else
            {
                // Fallback Heuristic Assistant nếu Gemini chưa cấu hình hoặc tạm lỗi
                (aiReplyText, matchedCards) = GenerateFallbackResponse(cleanMessage, candidateMovies);
            }

            // 7. Lưu tin nhắn người dùng và câu trả lời AI vào Redis
            var userMsg = new ChatMessageDto
            {
                Role = "user",
                Content = cleanMessage,
                Timestamp = DateTime.UtcNow
            };

            var aiMsg = new ChatMessageDto
            {
                Role = "model",
                Content = aiReplyText,
                Movies = matchedCards,
                Timestamp = DateTime.UtcNow
            };

            history.Add(userMsg);
            history.Add(aiMsg);

            // Giới hạn số lượng tin nhắn lưu trong lịch sử
            if (history.Count > _options.MaxHistoryMessages * 2)
            {
                history = history.Skip(history.Count - (_options.MaxHistoryMessages * 2)).ToList();
            }

            await _cache.SetAsync(
                historyKey,
                history,
                TimeSpan.FromMinutes(_options.HistoryExpirationMinutes),
                cancellationToken);

            return new ChatResponseDto
            {
                ConversationId = conversationId,
                Reply = aiReplyText,
                Movies = matchedCards,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<ChatHistoryDto> GetHistoryAsync(string conversationId, string userKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return new ChatHistoryDto { ConversationId = conversationId };
            }

            var historyKey = $"tflix:chat:history:{userKey}:{conversationId}";
            var history = await _cache.GetAsync<List<ChatMessageDto>>(historyKey, cancellationToken)
                          ?? new List<ChatMessageDto>();

            return new ChatHistoryDto
            {
                ConversationId = conversationId,
                Messages = history
            };
        }

        public async Task<bool> ClearHistoryAsync(string conversationId, string userKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return false;

            var historyKey = $"tflix:chat:history:{userKey}:{conversationId}";
            await _cache.RemoveAsync(historyKey, cancellationToken);
            return true;
        }

        // ==========================================
        // PRIVATE HELPERS
        // ==========================================

        private async Task EnforceRateLimitAsync(string userKey, CancellationToken ct)
        {
            var rateKey = $"tflix:chat:rate:{userKey}";
            try
            {
                var count = await _cache.GetAsync<int?>(rateKey, ct) ?? 0;
                if (count >= _options.MaxMessagesPerMinute)
                {
                    throw new InvalidOperationException("Bạn đã gửi tin nhắn quá nhanh. Vui lòng đợi 1 phút trước khi tiếp tục.");
                }

                await _cache.SetAsync(rateKey, count + 1, TimeSpan.FromMinutes(1), ct);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rate limit check failed on Redis for {UserKey}. Continuing without blocking.", userKey);
            }
        }

        private async Task<List<InternalMovieCandidate>> GetTFlixCatalogAsync(CancellationToken ct)
        {
            const string catalogCacheKey = "tflix:chat:catalog";
            var cached = await _cache.GetAsync<List<InternalMovieCandidate>>(catalogCacheKey, ct);
            if (cached != null && cached.Count > 0)
            {
                return cached;
            }

            var list = new List<InternalMovieCandidate>();

            try
            {
                // 1. Lấy Movies
                var movieResp = await _movieService.get_all();
                if (movieResp?.Data is DataSet ds && ds.Tables.Count > 0)
                {
                    var dt = ds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        var id = row.Table.Columns.Contains("moviE_ID") && row["moviE_ID"] != DBNull.Value
                            ? Convert.ToInt64(row["moviE_ID"])
                            : 0;
                        if (id <= 0) continue;

                        var title = row.Table.Columns.Contains("title") ? row["title"]?.ToString() ?? "" : "";
                        var originalTitle = row.Table.Columns.Contains("originaL_TITLE") ? row["originaL_TITLE"]?.ToString() : null;
                        var overview = row.Table.Columns.Contains("overview") ? row["overview"]?.ToString() : null;
                        var genres = row.Table.Columns.Contains("genres") ? row["genres"]?.ToString() : null;
                        var country = row.Table.Columns.Contains("countrY_CODE") ? row["countrY_CODE"]?.ToString() : null;
                        var poster = row.Table.Columns.Contains("posteR_URL") ? row["posteR_URL"]?.ToString() : null;

                        int? year = null;
                        if (row.Table.Columns.Contains("releasE_DATE") && row["releasE_DATE"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(row["releasE_DATE"].ToString(), out var dtVal))
                                year = dtVal.Year;
                        }

                        decimal rating = 8.5m;
                        if (row.Table.Columns.Contains("avG_RATING") && row["avG_RATING"] != DBNull.Value)
                        {
                            if (decimal.TryParse(row["avG_RATING"].ToString(), out var r))
                                rating = r;
                        }

                        list.Add(new InternalMovieCandidate
                        {
                            Id = id,
                            Title = title,
                            OriginalTitle = originalTitle,
                            Overview = overview,
                            Genres = genres,
                            CountryCode = country,
                            Year = year,
                            Rating = rating,
                            PosterUrl = poster,
                            Kind = "movie"
                        });
                    }
                }

                // 2. Lấy Series
                var seriesResp = await _seriesService.get_all();
                if (seriesResp?.Data is DataSet sds && sds.Tables.Count > 0)
                {
                    var dt = sds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        var id = row.Table.Columns.Contains("serieS_ID") && row["serieS_ID"] != DBNull.Value
                            ? Convert.ToInt64(row["serieS_ID"])
                            : 0;
                        if (id <= 0) continue;

                        var title = row.Table.Columns.Contains("title") ? row["title"]?.ToString() ?? "" : "";
                        var originalTitle = row.Table.Columns.Contains("originaL_TITLE") ? row["originaL_TITLE"]?.ToString() : null;
                        var overview = row.Table.Columns.Contains("overview") ? row["overview"]?.ToString() : null;
                        var genres = row.Table.Columns.Contains("genres") ? row["genres"]?.ToString() : null;
                        var country = row.Table.Columns.Contains("countrY_CODE") ? row["countrY_CODE"]?.ToString() : null;
                        var poster = row.Table.Columns.Contains("posteR_URL") ? row["posteR_URL"]?.ToString() : null;

                        int? year = null;
                        if (row.Table.Columns.Contains("firsT_AIR_DATE") && row["firsT_AIR_DATE"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(row["firsT_AIR_DATE"].ToString(), out var dtVal))
                                year = dtVal.Year;
                        }

                        list.Add(new InternalMovieCandidate
                        {
                            Id = id,
                            Title = title,
                            OriginalTitle = originalTitle,
                            Overview = overview,
                            Genres = genres,
                            CountryCode = country,
                            Year = year,
                            Rating = 8.8m,
                            PosterUrl = poster,
                            Kind = "series"
                        });
                    }
                }

                if (list.Count > 0)
                {
                    await _cache.SetAsync(catalogCacheKey, list, TimeSpan.FromMinutes(_options.CacheExpirationMinutes), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy Catalog phim từ Database.");
            }

            return list;
        }

        private List<InternalMovieCandidate> SelectCandidates(
            List<InternalMovieCandidate> catalog,
            string userMessage,
            List<ChatMessageDto> history)
        {
            if (catalog == null || catalog.Count == 0) return new List<InternalMovieCandidate>();

            var combinedText = userMessage.ToLowerInvariant();
            if (history != null && history.Count > 0)
            {
                var recentTexts = history.TakeLast(3).Select(h => h.Content.ToLowerInvariant());
                combinedText = string.Join(" ", recentTexts) + " " + combinedText;
            }

            // Từ điển phát hiện thể loại
            var genreKeywords = new Dictionary<string, string[]>
            {
                { "hành động", new[] { "hành động", "action", "đánh nhau", "rượt đuổi" } },
                { "kinh dị", new[] { "kinh dị", "horror", "ma", "ghê rợn", "quỷ", "dọa ma", "exhuma", "zombie" } },
                { "tình cảm", new[] { "tình cảm", "lãng mạn", "romance", "yêu đương", "tâm lý" } },
                { "viễn tưởng", new[] { "viễn tưởng", "sci-fi", "khoa học viễn tưởng", "quái vật", "godzilla", "kong", "avatar" } },
                { "hoạt hình", new[] { "hoạt hình", "anime", "animation", "suzume" } },
                { "gia đình", new[] { "gia đình", "lật mặt", "ước" } },
                { "lịch sử", new[] { "lịch sử", "chính kịch", "oppenheimer", "chiến tranh" } }
            };

            // Từ điển phát hiện quốc gia
            var countryKeywords = new Dictionary<string, string[]>
            {
                { "KR", new[] { "hàn quốc", "hàn", "korea", "korean", "xứ kim chi" } },
                { "US", new[] { "mỹ", "hollywood", "u.s", "us", "âu mỹ" } },
                { "VN", new[] { "việt nam", "việt", "phim việt", "sơn tùng", "mai", "lật mặt" } },
                { "JP", new[] { "nhật bản", "nhật", "anime", "japan" } }
            };

            var matchedGenres = new HashSet<string>();
            foreach (var kv in genreKeywords)
            {
                if (kv.Value.Any(k => combinedText.Contains(k)))
                {
                    matchedGenres.Add(kv.Key);
                }
            }

            var matchedCountries = new HashSet<string>();
            foreach (var kv in countryKeywords)
            {
                if (kv.Value.Any(k => combinedText.Contains(k)))
                {
                    matchedCountries.Add(kv.Key);
                }
            }

            // Tính điểm tương đồng cho từng phim
            var scored = catalog.Select(m =>
            {
                int score = 0;
                var mGenres = (m.Genres ?? "").ToLowerInvariant();
                var mTitle = m.Title.ToLowerInvariant();
                var mOrigTitle = (m.OriginalTitle ?? "").ToLowerInvariant();
                var mOverview = (m.Overview ?? "").ToLowerInvariant();

                // Trùng thể loại (+3 điểm)
                foreach (var g in matchedGenres)
                {
                    if (mGenres.Contains(g)) score += 3;
                }

                // Trùng quốc gia (+3 điểm)
                if (!string.IsNullOrEmpty(m.CountryCode) && matchedCountries.Contains(m.CountryCode.ToUpperInvariant()))
                {
                    score += 3;
                }

                // Trùng từ khóa trong tiêu đề (+5 điểm)
                var words = userMessage.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    if (w.Length > 2)
                    {
                        if (mTitle.Contains(w) || mOrigTitle.Contains(w)) score += 4;
                        if (mOverview.Contains(w)) score += 1;
                    }
                }

                // Điểm rating cơ bản (+1 điểm cho rating >= 8.5)
                if (m.Rating >= 8.5m) score += 1;

                return new { Movie = m, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Movie.Rating)
            .Take(12) // Giới hạn tối đa 12 phim ứng viên để tối ưu token
            .Select(x => x.Movie)
            .ToList();

            return scored;
        }

        private string BuildSystemPrompt(List<InternalMovieCandidate> candidates)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là TFlix AI, trợ lý tư vấn phim thông minh của nền tảng xem phim TFlix.");
            sb.AppendLine("Nhiệm vụ của bạn là giúp người dùng tìm được bộ phim phù hợp với sở thích, tâm trạng và nhu cầu giải trí.");
            sb.AppendLine();
            sb.AppendLine("Nguyên tắc bắt buộc:");
            sb.AppendLine("1. Luôn trả lời bằng tiếng Việt tự nhiên, thân thiện và lịch thiệp.");
            sb.AppendLine("2. ƯU TIÊN ĐỀ XUẤT PHIM CÓ TRONG DANH SÁCH DO HỆ THỐNG TFLIX CUNG CẤP DƯỚI ĐÂY.");
            sb.AppendLine("3. KHÔNG TỰ BỊA TÊN PHIM, ID PHIM, POSTER HOẶC ĐƯỜNG DẪN KHÔNG CÓ TRONG HỆ THỐNG.");
            sb.AppendLine("4. Khi đề xuất bất kỳ bộ phim nào từ danh sách dưới đây, hãy kèm thẻ định dạng `[MOVIE_SUGGESTION: id=ID, kind=KIND]` ngay cạnh tên phim hoặc cuối lời giới thiệu về phim đó.");
            sb.AppendLine("5. Mỗi lần nên gợi ý khoảng 3–5 phim phù hợp, giải thích ngắn gọn tại sao phim đó đáng xem.");
            sb.AppendLine("6. Không tiết lộ nội dung quan trọng (spoilers) nếu người dùng không yêu cầu.");
            sb.AppendLine("7. Ghi nhớ sở thích trong phạm vi lịch sử hội thoại được cung cấp.");
            sb.AppendLine("8. Nếu không có phim nào trong danh sách khớp chính xác, hãy gợi ý phim gần giống nhất hoặc đề xuất mở rộng tiêu chí tìm kiếm.");
            sb.AppendLine();
            sb.AppendLine("DANH SÁCH PHIM ĐANG CÓ TRÊN NỀN TẢNG TFLIX:");

            if (candidates != null && candidates.Count > 0)
            {
                foreach (var c in candidates)
                {
                    sb.AppendLine($"- [ID: {c.Id} | Loại: {c.Kind}] Tên: {c.Title} ({c.OriginalTitle ?? ""}) | Năm: {c.Year} | Thể loại: {c.Genres} | Quốc gia: {c.CountryCode} | Đánh giá: {c.Rating}★ | Mô tả: {c.Overview}");
                }
            }
            else
            {
                sb.AppendLine("(Hiện chưa có phim nào trong danh mục này)");
            }

            return sb.ToString();
        }

        private (string CleanReply, List<ChatMovieCardDto> Movies) ExtractMovieSuggestions(
            string rawReply,
            List<InternalMovieCandidate> candidates)
        {
            var movies = new List<ChatMovieCardDto>();
            var seenIds = new HashSet<string>();

            // Regex trích xuất [MOVIE_SUGGESTION: id=..., kind=...]
            var pattern = @"\[MOVIE_SUGGESTION:\s*id=(\d+),\s*kind=(\w+)\]";
            var matches = Regex.Matches(rawReply, pattern, RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                if (long.TryParse(m.Groups[1].Value, out var id))
                {
                    var kind = m.Groups[2].Value.ToLowerInvariant();
                    var key = $"{kind}_{id}";
                    if (!seenIds.Contains(key))
                    {
                        seenIds.Add(key);
                        var cand = candidates.FirstOrDefault(c => c.Id == id && string.Equals(c.Kind, kind, StringComparison.OrdinalIgnoreCase));
                        if (cand != null)
                        {
                            movies.Add(MapToCardDto(cand));
                        }
                    }
                }
            }

            // Loại bỏ thẻ [MOVIE_SUGGESTION: ...] khỏi văn bản để người dùng đọc tự nhiên
            var cleanReply = Regex.Replace(rawReply, pattern, "", RegexOptions.IgnoreCase).Trim();

            // Nếu Gemini không tạo thẻ định dạng nhưng nhắc đến tên phim trong danh sách
            if (movies.Count == 0 && candidates != null)
            {
                foreach (var c in candidates)
                {
                    if (cleanReply.IndexOf(c.Title, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var key = $"{c.Kind}_{c.Id}";
                        if (!seenIds.Contains(key))
                        {
                            seenIds.Add(key);
                            movies.Add(MapToCardDto(c));
                            if (movies.Count >= 5) break;
                        }
                    }
                }
            }

            return (cleanReply, movies);
        }

        private (string Reply, List<ChatMovieCardDto> Movies) GenerateFallbackResponse(
            string userMessage,
            List<InternalMovieCandidate> candidates)
        {
            var recommended = candidates.Take(4).ToList();
            var cards = recommended.Select(MapToCardDto).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Dạ chào bạn! Dựa trên yêu cầu của bạn, TFlix AI xin gợi ý các tác phẩm đặc sắc đang có sẵn trên nền tảng:");
            sb.AppendLine();

            foreach (var m in recommended)
            {
                sb.AppendLine($"🎬 **{m.Title}** ({m.Year}) — {m.Genres}");
                if (!string.IsNullOrWhiteSpace(m.Overview))
                {
                    var shortOverview = m.Overview.Length > 120 ? m.Overview.Substring(0, 117) + "..." : m.Overview;
                    sb.AppendLine($"   _{shortOverview}_");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Bạn có thể bấm trực tiếp vào thẻ phim bên dưới để xem phim ngay với chất lượng 4K chuẩn HDR nhé! Bạn muốn tìm hiểu thêm về bộ phim nào không ạ?");

            return (sb.ToString(), cards);
        }

        private static ChatMovieCardDto MapToCardDto(InternalMovieCandidate c)
        {
            var detailUrl = string.Equals(c.Kind, "series", StringComparison.OrdinalIgnoreCase)
                ? $"/Film/Watch?id={c.Id}&kind=series"
                : $"/Film/Watch?id={c.Id}&kind=movie";

            return new ChatMovieCardDto
            {
                Id = c.Id,
                Kind = c.Kind,
                Title = c.Title,
                OriginalTitle = c.OriginalTitle,
                PosterUrl = !string.IsNullOrWhiteSpace(c.PosterUrl)
                    ? c.PosterUrl
                    : "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop",
                Rating = c.Rating,
                Genres = c.Genres,
                Year = c.Year,
                DetailUrl = detailUrl,
                Description = c.Overview
            };
        }
    }
}
