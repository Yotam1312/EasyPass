using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyPass.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    // PIN stored as a BCrypt hash for security
    [Required]
    [MaxLength(200)]
    public string PinHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // How many consecutive failed login attempts this user has made
    public int FailedLoginCount { get; set; } = 0;

    // When the timed lockout expires. Null means not currently locked.
    public DateTime? LockoutEndAt { get; set; } = null;

    // True when the account is permanently locked after too many failures.
    // Requires support to unlock.
    public bool IsPermanentlyLocked { get; set; } = false;

    // A user can have multiple saved passwords
    public ICollection<PasswordEntry> Passwords { get; set; } = new List<PasswordEntry>();
}
