using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Interfaces
{
    public interface ISupabaseService
    {
      
            Task<string?> UploadFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string pathOrUrl);
    }
}
