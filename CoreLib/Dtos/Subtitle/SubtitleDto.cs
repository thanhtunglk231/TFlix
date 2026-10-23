using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreLib.Dtos.Subtitle
{
    public sealed class SubtitleDto
    {
        public long SubtitleId { get; set; }
        public long SourceId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Format { get; set; } = "VTT";
        public string Url { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public sealed class CreateSubtitleDto
    {
        public long SourceId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Format { get; set; } = "VTT";
        public string Url { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public sealed class GeminiSourceSubtitleCue
    {
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string Vi { get; set; } = string.Empty;
        public string En { get; set; } = string.Empty;
    }

    public sealed class BrowserSubtitleJobRequestDto
    {
        public long SourceId { get; set; }
        public int TotalChunks { get; set; }
        public bool ForceRegenerate { get; set; }
    }

    public sealed class BrowserSubtitleJobDto
    {
        public Guid JobId { get; set; }
        public long SourceId { get; set; }
        public string Status { get; set; } = "Pending";
        public int TotalChunks { get; set; }
        public List<int> CompletedChunks { get; set; } = new();
        public string? Message { get; set; }
        public string? SubtitleUrl { get; set; }
    }

    public sealed class BrowserSubtitleChunkResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int CueCount { get; set; }
    }

    public sealed class SubtitleDraftDto
    {
        public Guid JobId { get; set; }
        public long SourceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public List<GeminiSourceSubtitleCue> Cues { get; set; } = new();
    }

    public sealed class UpdateSubtitleDraftDto
    {
        public List<GeminiSourceSubtitleCue> Cues { get; set; } = new();
    }

    public sealed class SaveSourceSubtitlesDto
    {
        public long SourceId { get; set; }
        public List<GeminiSourceSubtitleCue> Cues { get; set; } = new();
    }


    public sealed class SubtitleCueDto
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;

        [JsonPropertyName("startSeconds")]
        public double StartSeconds { get; set; }

        [JsonPropertyName("endSeconds")]
        public double EndSeconds { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public sealed class SubtitleTrackDto
    {
        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        [JsonPropertyName("languageName")]
        public string LanguageName { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = "VTT";

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("vttUrl")]
        public string? VttUrl { get; set; }

        [JsonPropertyName("cues")]
        public List<SubtitleCueDto> Cues { get; set; } = new();
    }

    public sealed class GenerateSubtitleRequestDto
    {
        [JsonPropertyName("movieId")]
        public long? MovieId { get; set; }

        [JsonPropertyName("episodeId")]
        public long? EpisodeId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("originalTitle")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("genres")]
        public string? Genres { get; set; }

        [JsonPropertyName("durationSeconds")]
        public double? DurationSeconds { get; set; }

        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }

        [JsonPropertyName("forceRegenerate")]
        public bool ForceRegenerate { get; set; }
    }

    public sealed class GenerateSubtitleResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("tracks")]
        public List<SubtitleTrackDto> Tracks { get; set; } = new();
    }

    public sealed class GeminiSubtitlePayload
    {
        [JsonPropertyName("subtitles")]
        public List<GeminiSubtitleTrack>? Subtitles { get; set; }
    }

    public sealed class GeminiSubtitleTrack
    {
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("languageName")]
        public string? LanguageName { get; set; }

        [JsonPropertyName("cues")]
        public List<GeminiSubtitleCue>? Cues { get; set; }
    }

    public sealed class GeminiSubtitleCue
    {
        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("end")]
        public string? End { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
