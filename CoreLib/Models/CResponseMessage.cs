using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models
{
    public class CResponseMessage
    {
        public string code { get; set; }
        public bool Success { get; set; }
        public string message { get; set; }
        public object? Data { get; set; }
    }
}
