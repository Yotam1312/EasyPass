namespace EasyPass.API.Models
{
    public class PasswordEntry
    {
        public int Id { get; set; }

        public string Service { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string EncryptedPassword { get; set; } = string.Empty;

        // Reference to the user who owns this password
        public int UserId { get; set; }

        // Navigation property (optional when creating a new entry)
        public User? User { get; set; }
    }
}
