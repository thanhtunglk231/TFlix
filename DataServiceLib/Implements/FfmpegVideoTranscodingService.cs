using System.Diagnostics;
using System.ComponentModel;
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
            _ffmpegPath = ResolveExecutablePath(
                configuration["FFmpeg:Path"],
                "FFMPEG_PATH",
                OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
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
            var processStarted = false;
            var stopwatch = Stopwatch.StartNew();
            var duration = await GetDurationAsync(sourcePath, cancellationToken);
            _logger.LogInformation("Starting MP4-to-HLS packaging");

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Không khởi động được FFmpeg.");

                processStarted = true;
                var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                var progressTask = ReadProgressAsync(process.StandardOutput, duration, progress, cancellationToken);

                await Task.WhenAll(standardErrorTask, progressTask, process.WaitForExitAsync(cancellationToken));
                var standardError = await standardErrorTask;

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
                if (processStarted)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException) { /* process is no longer associated */ }
                    catch (Exception killException)
                    {
                        _logger.LogWarning(killException, "Could not stop FFmpeg process after failure");
                    }
                }
                _logger.LogError(ex, "MP4-to-HLS packaging failed");

                if (!processStarted && ex is Win32Exception)
                {
                    throw new InvalidOperationException(
                        $"Khong khoi dong duoc FFmpeg tai '{_ffmpegPath}'. " +
                        "Hay cai FFmpeg tren server va them vao PATH, dat bien moi truong FFMPEG_PATH, " +
                        "hoac cau hinh FFmpeg:Path tro den file thuc thi FFmpeg.",
                        ex);
                }
                throw;
            }
        }

        private async Task<TimeSpan?> GetDurationAsync(string sourcePath, CancellationToken cancellationToken)
        {
            var ffprobeFileName = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
            var configuredDirectory = Path.GetDirectoryName(_ffmpegPath);
            var ffprobePath = string.IsNullOrWhiteSpace(configuredDirectory)
                ? ResolveExecutablePath(null, "FFPROBE_PATH", ffprobeFileName)
                : Path.Combine(configuredDirectory, ffprobeFileName);

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

        private static string ResolveExecutablePath(
            string? configuredPath,
            string environmentVariable,
            string executableName)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath.Trim();

            var environmentPath = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentPath))
                return environmentPath.Trim();

            var appLocalCandidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, executableName),
                Path.Combine(AppContext.BaseDirectory, "ffmpeg", executableName),
                Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", executableName)
            };
            return appLocalCandidates.FirstOrDefault(File.Exists) ?? executableName;
        }

        private static async Task ReadProgressAsync(
            StreamReader reader,
            TimeSpan? duration,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var lastReportedPercent = -1;
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
                    var clamped = (int)Math.Clamp(Math.Floor(percent), 0, 99);
                    if (clamped > lastReportedPercent)
                    {
                        lastReportedPercent = clamped;
                        progress?.Report(clamped);
                    }
                }
            }
        }
    }
}
