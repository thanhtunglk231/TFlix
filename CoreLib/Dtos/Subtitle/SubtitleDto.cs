using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreLib.Dtos.Subtitle
{
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
