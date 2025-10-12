namespace EasyPass.App.Models
{
    public class PasswordEntry
    {
        public int Id { get; set; }
        public string Service { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;

        // Used for linking the entry to its user (for API compatibility)
        public int UserId { get; set; }

        // Optional navigation property
        public User? User { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;

        public string PinHash { get; set; } = string.Empty;
        public string PinSalt { get; set; } = string.Empty;

        // A user can have multiple password entries
        public ICollection<PasswordEntry> Passwords { get; set; } = new List<PasswordEntry>();
    }
}
