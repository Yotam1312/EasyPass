using System.ComponentModel.DataAnnotations;

namespace EasyPass.API.Models
{
    public class RegisterRequest
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
}
