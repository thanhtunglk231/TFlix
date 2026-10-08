using System.ComponentModel.DataAnnotations;

namespace CoreLib.Dtos
{
    public class CreateAdminAccountDto
    {
        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public List<long> RoleIds { get; set; } = new();
    }
}
