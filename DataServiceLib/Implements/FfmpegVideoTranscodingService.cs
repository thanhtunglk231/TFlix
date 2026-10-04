using System.Diagnostics;
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
                "-hide_banner", "-loglevel", "warning", "-y", "-i", sourcePath,
                "-map", "0:v:0", "-map", "0:a:0?", "-c:v", "copy", "-c:a", "aac",
                "-b:a", "128k", "-f", "hls", "-hls_time", "6", "-hls_playlist_type", "vod",
                "-hls_list_size", "0", "-hls_segment_filename", segmentPattern, playlistPath
            })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting MP4-to-HLS packaging");

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Không khởi động được FFmpeg.");

                var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                var standardError = await standardErrorTask;
                await standardOutputTask;

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"FFmpeg kết thúc với mã {process.ExitCode}: {standardError.Trim()}");

                if (!File.Exists(playlistPath) || !Directory.EnumerateFiles(outputDirectory, "*.ts").Any())
                    throw new InvalidOperationException("FFmpeg không tạo được playlist hoặc segment HLS.");

                _logger.LogInformation(
                    "MP4-to-HLS packaging completed in {ElapsedMilliseconds} ms",
                    stopwatch.ElapsedMilliseconds);
                return playlistPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MP4-to-HLS packaging failed");
                throw;
            }
        }
    }
}