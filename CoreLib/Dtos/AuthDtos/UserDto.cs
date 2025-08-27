using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Dtos.AuthDtos
{
    public class UserDto
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string? CountryCode { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsEmailVerified { get; set; }
        public string Status { get; set; }
    }

}
