using System.Collections.Concurrent;
using CoreLib.Dtos.VideSoure;

namespace Server.Services
{
    public sealed class VideoUploadProgressTracker
    {
        private static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(30);
        private readonly ConcurrentDictionary<Guid, ProgressEntry> _entries = new();

        public void SetStage(Guid uploadId, string stage, string message)
        {
            Update(uploadId, current => current with { Stage = stage, Message = message });
        }

        public void StartSegmentUpload(Guid uploadId, int totalSegments, int segmentsAlreadyUploaded = 0)
        {
            Update(uploadId, current => current with
            {
                Stage = "uploading-segments",
                Message = "Đang tải HLS segments lên storage",
                SegmentsUploaded = Math.Clamp(segmentsAlreadyUploaded, 0, totalSegments),
                TotalSegments = totalSegments
            });
        }

        public VideoUploadProgressDto IncrementUploadedSegment(Guid uploadId)
        {
            var entry = _entries.AddOrUpdate(
                uploadId,
                _ => new ProgressEntry(new VideoUploadProgressDto("uploading-segments", "Đang tải HLS segments lên storage", 1, 1), DateTimeOffset.UtcNow),
                (_, current) => current with
                {
                    Progress = current.Progress with
                    {
                        SegmentsUploaded = Math.Min(current.Progress.SegmentsUploaded + 1, current.Progress.TotalSegments)
                    },
                    UpdatedAt = DateTimeOffset.UtcNow
                });

            return entry.Progress;
        }

        public bool TryGetProgress(Guid uploadId, out VideoUploadProgressDto progress)
        {
            RemoveExpiredEntries();
            if (_entries.TryGetValue(uploadId, out var entry))
            {
                progress = entry.Progress;
                return true;
            }

            progress = new VideoUploadProgressDto("not-found", "Không tìm thấy upload đang chạy.", 0, 0);
            return false;
        }

        public void Remove(Guid uploadId)
        {
            _entries.TryRemove(uploadId, out _);
        }

        private void Update(Guid uploadId, Func<VideoUploadProgressDto, VideoUploadProgressDto> update)
        {
            _entries.AddOrUpdate(
                uploadId,
                _ => new ProgressEntry(update(new VideoUploadProgressDto("starting", "Đang khởi tạo upload", 0, 0)), DateTimeOffset.UtcNow),
                (_, current) => new ProgressEntry(update(current.Progress), DateTimeOffset.UtcNow));
            RemoveExpiredEntries();
        }

        private void RemoveExpiredEntries()
        {
            var expiredBefore = DateTimeOffset.UtcNow - EntryLifetime;
            foreach (var entry in _entries)
            {
                if (entry.Value.UpdatedAt < expiredBefore)
                    _entries.TryRemove(entry.Key, out _);
            }
        }

        private sealed record ProgressEntry(VideoUploadProgressDto Progress, DateTimeOffset UpdatedAt);
    }
}