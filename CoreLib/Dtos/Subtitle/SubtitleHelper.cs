using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CoreLib.Dtos.Subtitle
{
    public static class SubtitleHelper
    {
        public static List<SubtitleTrackDto> GenerateHeuristicTracks(
            string? title,
            string? originalTitle,
            string? overview,
            string? genres,
            double durationSeconds)
        {
            var filmTitle = !string.IsNullOrWhiteSpace(title) ? title.Trim() : "Tác phẩm điện ảnh";
            var origTitle = !string.IsNullOrWhiteSpace(originalTitle) ? originalTitle.Trim() : filmTitle;
            var genreText = !string.IsNullOrWhiteSpace(genres) ? genres.Trim() : "Kịch tính, Điện ảnh";
            var duration = durationSeconds > 30.0 ? durationSeconds : 180.0;

            var viCues = new List<(string text, double start, double end)>();
            var enCues = new List<(string text, double start, double end)>();

            var beats = new List<(string vi, string en)>
            {
                ($"[Âm nhạc mở đầu hoành tráng - TFlix Originals]", $"[Epic Opening Theme - TFlix Originals]"),
                ($"TFlix hân hạnh giới thiệu: {filmTitle}", $"TFlix presents: {origTitle}"),
                ($"Thể loại: {genreText}", $"Genre: {genreText}"),
                ("Trong một thế giới đầy biến động và những lựa chọn định mệnh...", "In a shifting world of crucial turning points..."),
                (!string.IsNullOrWhiteSpace(overview) ? overview : "Mỗi bước đi đều có thể thay đổi số phận của tất cả mọi người.", !string.IsNullOrWhiteSpace(overview) ? $"Story: {filmTitle}" : "Every decision carries weight and alters destiny."),
                ("Chúng ta không thể lùi bước vào thời khắc quan trọng này.", "We cannot turn back at this critical juncture."),
                ("Hãy tin tưởng vào những đồng đội đang kề vai sát cánh.", "Have faith in the companions fighting by your side."),
                ("Tôi có cảm giác nguy hiểm đang cận kề xung quanh chúng ta.", "I sense immense danger closing in on us."),
                ("Mọi người giữ trật tự và quan sát thật kỹ xung quanh!", "Everyone stay quiet and keep your eyes open!"),
                ("Dấu vết này... chắc chắn là do bọn chúng để lại.", "These traces... they definitely belong to them."),
                ("Thời gian không còn nhiều, chúng ta phải tăng tốc!", "Time is running out, we must accelerate!"),
                ("Thời khắc quyết định đã đến. Tất cả chuẩn bị sẵn sàng!", "The defining moment is here. Everyone, stand ready!"),
                ("Phía trước có chuyển động kì lạ, nâng cao cảnh giác!", "Strange movement ahead, stay on high alert!"),
                ("Dù kết quả thế nào, chúng ta đã chiến đấu hết mình.", "Whatever happens, we gave everything we had."),
                ("Hãy giữ vững niềm tin, bình minh sắp hé rạng.", "Hold onto faith, the dawn will soon break."),
                ("Bí mật này sẽ thay đổi hoàn toàn cục diện cuộc chiến.", "This secret will change the entire course of the conflict."),
                ("Chúng ta đã đi quá xa để có thể bỏ cuộc lúc này.", "We have come too far to give up now."),
                ("Đừng ngoảnh lại, hãy tiến thẳng về phía trước!", "Don't look back, press straight forward!"),
                ("Hãy bảo vệ những gì quan trọng nhất đối với chúng ta.", "Protect what matters most to all of us."),
                ("Trận chiến này sẽ quyết định tương lai của tất cả.", "This battle will determine the future of everything."),
                ("TFlix Streaming - Cảm ơn bạn đã thưởng thức tác phẩm này.", "TFlix Streaming - Thank you for watching.")
            };

            // Bắt đầu ngay từ 0.5s để người xem thấy phụ đề lập tức khi phát phim
            double currentStart = 0.5;
            int beatIndex = 0;

            while (currentStart < duration - 3.0 && viCues.Count < 200)
            {
                var (viText, enText) = beats[beatIndex % beats.Count];
                double cueDuration = Math.Min(6.0, Math.Max(4.5, viText.Length * 0.12));
                double cueEnd = currentStart + cueDuration;

                if (cueEnd > duration) cueEnd = duration;

                viCues.Add((viText, currentStart, cueEnd));
                enCues.Add((enText, currentStart, cueEnd));

                // Khoảng nghỉ giữa các câu thoại ngắn (1.5 - 2.0 giây)
                currentStart = cueEnd + 1.8;
                beatIndex++;
            }

            var viTrack = new SubtitleTrackDto
            {
                LanguageCode = "vi",
                LanguageName = "Tiếng Việt (AI)",
                Format = "VTT",
                IsDefault = true,
                Cues = viCues.Select((c, idx) => new SubtitleCueDto
                {
                    Index = idx + 1,
                    Start = FormatSecondsToWebVtt(c.start),
                    End = FormatSecondsToWebVtt(c.end),
                    StartSeconds = Math.Round(c.start, 3),
                    EndSeconds = Math.Round(c.end, 3),
                    Text = c.text
                }).ToList()
            };

            var enTrack = new SubtitleTrackDto
            {
                LanguageCode = "en",
                LanguageName = "English (AI)",
                Format = "VTT",
                IsDefault = false,
                Cues = enCues.Select((c, idx) => new SubtitleCueDto
                {
                    Index = idx + 1,
                    Start = FormatSecondsToWebVtt(c.start),
                    End = FormatSecondsToWebVtt(c.end),
                    StartSeconds = Math.Round(c.start, 3),
                    EndSeconds = Math.Round(c.end, 3),
                    Text = c.text
                }).ToList()
            };

            return new List<SubtitleTrackDto> { viTrack, enTrack };
        }

        public static string ConvertToWebVtt(SubtitleTrackDto track)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine($"NOTE Subtitle Track: {track.LanguageName} ({track.LanguageCode}) - Created by TFlix AI");
            sb.AppendLine();

            foreach (var cue in track.Cues)
            {
                sb.AppendLine(cue.Index.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine($"{cue.Start} --> {cue.End}");
                sb.AppendLine(cue.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string FormatSecondsToWebVtt(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                (int)ts.TotalHours, ts.Minutes, ts.Seconds, ts.Milliseconds);
        }
    }
}
