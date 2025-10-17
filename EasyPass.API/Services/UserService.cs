using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;
using EasyPass.API.Models;

namespace EasyPass.API.Services;

public class UserService
{
    private readonly EasyPassContext _context;

    public UserService(EasyPassContext context)
    {
        _context = context;
    }

    // Registers a new user with a salted and hashed PIN.
    public async Task<User?> RegisterAsync(string username, string pin)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return null;

        var salt = GenerateSalt();
        var hash = HashPin(pin, salt);

        var user = new User
        {
            Username = username,
            PinHash = hash,
            PinSalt = salt
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    // Validates user login by comparing stored hash with input PIN hash.
    public async Task<User?> LoginAsync(string username, string pin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        var hash = HashPin(pin, user.PinSalt);

        return user.PinHash == hash ? user : null;
    }

    // Generates a random salt value.
    private static string GenerateSalt()
    {
        var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[16];
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    // Combines PIN and salt and hashes them using SHA-256.
    private static string HashPin(string pin, string salt)
    {
        using var sha256 = SHA256.Create();
        var combined = Encoding.UTF8.GetBytes(pin + salt);
        var hashBytes = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hashBytes);
    }
}
