using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models
{
    public class UploadOriginalVideoForm
    {
        public IFormFile File { get; set; } = default!;

        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }
    }
}
