using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos
{
    public class AddVideoSourcePartDto
    {
        public decimal SourceId { get; set; }
        public int PartIndex { get; set; }
        public string Url { get; set; } = string.Empty;
        public long? ByteSize { get; set; }
        public int? DurationSec { get; set; }
        public string? Checksum { get; set; }
    }
}
