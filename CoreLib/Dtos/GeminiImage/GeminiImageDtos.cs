using System;

namespace CoreLib.Dtos.GeminiImage
{
    public class GeminiGenerateImageRequestDto
    {
        public string Prompt { get; set; } = string.Empty;
        public string? AspectRatio { get; set; } = "16:9"; // "16:9", "3:4" (poster 2:3), "1:1", "9:16", "4:3"
        public string? Model { get; set; } = "imagen-3.0-generate-002";
        public string? ApiKey { get; set; }
        public string? NegativePrompt { get; set; }
    }

    public class GeminiGenerateImageResponseDto
    {
        public bool Success { get; set; }
        public string? Base64Data { get; set; }
        public string? MimeType { get; set; } = "image/jpeg";
        public string? ImageUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ModelUsed { get; set; }
    }

    public class GeminiEnhancePromptRequestDto
    {
        public string Description { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Style { get; set; } = "cinematic"; // cinematic, poster, anime, cyberpunk, horror, action, scifi
        public string? ApiKey { get; set; }
    }

    public class GeminiEnhancePromptResponseDto
    {
        public bool Success { get; set; }
        public string? EnhancedPrompt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GeminiSaveAssetRequestDto
    {
        public string OwnerType { get; set; } = "MOVIE"; // "MOVIE", "SERIES", "EPISODE"
        public long OwnerId { get; set; }
        public string AssetType { get; set; } = "POSTER"; // "POSTER", "BACKDROP", "THUMB", "BANNER", "STILL"
        public string Base64Data { get; set; } = string.Empty;
        public string? MimeType { get; set; } = "image/jpeg";
        public int SortOrder { get; set; } = 0;
    }

    public class GeminiAutoBindRequestDto
    {
        public long MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public string? Overview { get; set; }
        public string? Genres { get; set; }
        public string? Style { get; set; } = "cinematic";
        public string? ApiKey { get; set; }
        public bool GeneratePoster { get; set; } = true;
        public bool GenerateBackdrop { get; set; } = true;
        public bool GenerateThumb { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }

    public class GeminiAutoBindResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? MovieTitle { get; set; }
        public long MovieId { get; set; }
        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }
        public string? ThumbUrl { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new();
    }
}
