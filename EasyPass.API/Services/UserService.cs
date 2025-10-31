using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;
using EasyPass.API.Models;
using BCrypt.Net;

namespace EasyPass.API.Services;

public class UserService
{
    private readonly EasyPassContext _context;

    public UserService(EasyPassContext context)
    {
        _context = context;
    }

    // Registers a new user with a BCrypt-hashed PIN.
    public async Task<User?> RegisterAsync(string username, string pin)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return null;

        // Hash the PIN using BCrypt (includes salt)
        var hash = BCrypt.Net.BCrypt.HashPassword(pin);

        var user = new User
        {
            Username = username,
            PinHash = hash,
            
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    // Validates user login by verifying the BCrypt hash.
    public async Task<User?> LoginAsync(string username, string pin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        var isValid = BCrypt.Net.BCrypt.Verify(pin, user.PinHash);

        return isValid ? user : null;
    }
}
