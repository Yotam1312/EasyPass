namespace EasyPass.API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    // PIN stored as a salted hash for security
    public string PinHash { get; set; } = string.Empty;
    public string PinSalt { get; set; } = string.Empty;

    // A user can have multiple saved passwords
    public ICollection<PasswordEntry> Passwords { get; set; } = new List<PasswordEntry>();
}
