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

    public DateTime CreatedAt { get; set; } = DateTime.Now;
   
    // A user can have multiple saved passwords
    public ICollection<PasswordEntry> Passwords { get; set; } = new List<PasswordEntry>();
}
