using System;
using System.ComponentModel.DataAnnotations;

namespace EasyPass.API.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "PIN must be at least 6 digits.")]
        [MaxLength(50)]
        public string Pin { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
