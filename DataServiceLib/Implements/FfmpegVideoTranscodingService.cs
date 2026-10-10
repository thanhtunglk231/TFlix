using System.Diagnostics;
using System.Globalization;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataServiceLib.Implements
{
    public class FfmpegVideoTranscodingService : IVideoTranscodingService
    {
        private readonly string _ffmpegPath;
        private readonly ILogger<FfmpegVideoTranscodingService> _logger;

        public FfmpegVideoTranscodingService(
            IConfiguration configuration,
            ILogger<FfmpegVideoTranscodingService> logger)
        {
            _ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg";
            _logger = logger;
        }

        public async Task<string> CreateHlsAsync(
            string sourcePath,
            string outputDirectory,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(outputDirectory);
            var playlistPath = Path.Combine(outputDirectory, "index.m3u8");
            var segmentPattern = Path.Combine(outputDirectory, "segment_%05d.ts");

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var argument in new[]
            {
                "-hide_banner", "-loglevel", "warning", "-progress", "pipe:1", "-nostats", "-y", "-i", sourcePath,
                "-map", "0:v:0", "-map", "0:a:0?", "-c:v", "copy", "-c:a", "aac",
                "-b:a", "128k", "-f", "hls", "-hls_time", "6", "-hls_playlist_type", "vod",
                "-hls_list_size", "0", "-hls_segment_filename", segmentPattern, playlistPath
            })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            var stopwatch = Stopwatch.StartNew();
            var duration = await GetDurationAsync(sourcePath, cancellationToken);
            _logger.LogInformation("Starting MP4-to-HLS packaging");

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Không khởi động được FFmpeg.");

                var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                var progressTask = ReadProgressAsync(process.StandardOutput, duration, progress, cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                var standardError = await standardErrorTask;
                await progressTask;

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"FFmpeg kết thúc với mã {process.ExitCode}: {standardError.Trim()}");

                if (!File.Exists(playlistPath) || !Directory.EnumerateFiles(outputDirectory, "*.ts").Any())
                    throw new InvalidOperationException("FFmpeg không tạo được playlist hoặc segment HLS.");

                _logger.LogInformation(
                    "MP4-to-HLS packaging completed in {ElapsedMilliseconds} ms",
                    stopwatch.ElapsedMilliseconds);
                progress?.Report(100);
                return playlistPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MP4-to-HLS packaging failed");
                throw;
            }
        }

        private async Task<TimeSpan?> GetDurationAsync(string sourcePath, CancellationToken cancellationToken)
        {
            var ffprobePath = Path.Combine(
                Path.GetDirectoryName(_ffmpegPath) ?? string.Empty,
                OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");
            if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(_ffmpegPath)))
                ffprobePath = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", sourcePath })
                startInfo.ArgumentList.Add(argument);

            try
            {
                using var process = new Process { StartInfo = startInfo };
                if (!process.Start()) return null;
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0
                    && double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                    && seconds > 0
                        ? TimeSpan.FromSeconds(seconds)
                        : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine video duration with FFprobe");
                return null;
            }
        }

        private static async Task ReadProgressAsync(
            StreamReader reader,
            TimeSpan? duration,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                if (line.Equals("progress=end", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report(100);
                    continue;
                }

                if (!line.StartsWith("out_time_us=", StringComparison.OrdinalIgnoreCase) || duration is null)
                    continue;

                if (long.TryParse(line["out_time_us=".Length..], out var microseconds))
                {
                    var percent = microseconds / (duration.Value.TotalSeconds * 1_000_000d) * 100d;
                    progress?.Report(Math.Clamp(percent, 0, 99));
                }
            }
        }
    }
}
