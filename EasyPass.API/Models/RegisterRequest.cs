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
        [MinLength(4)]
        [MaxLength(50)]
        public string Pin { get; set; } = string.Empty;
    }
}
