using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyPass.API.Models
{
    public class PasswordEntry
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Service { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string EncryptedPassword { get; set; } = string.Empty;

        // Reference to the user who owns this password
        public int UserId { get; set; }

        // Navigation property (optional when creating a new entry)
        public User? User { get; set; }

    }
}
