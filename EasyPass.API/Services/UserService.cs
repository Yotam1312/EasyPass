using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;
using EasyPass.API.Models;

namespace EasyPass.API.Services;

public class UserService
{
    // List of PINs that are too common and should not be allowed
    private static readonly HashSet<string> WeakPins = new HashSet<string>
    {
        "123456", "654321", "234567", "345678", "456789",
        "000000", "111111", "222222", "333333", "444444",
        "555555", "666666", "777777", "888888", "999999",
        "123123", "112233", "121212"
    };

    private readonly EasyPassContext _context;
    private readonly LoginAttemptService _loginAttemptService;

    public UserService(EasyPassContext context, LoginAttemptService loginAttemptService)
    {
        _context = context;
        _loginAttemptService = loginAttemptService;
    }

    // Registers a new user with a BCrypt-hashed PIN.
    public async Task<User?> RegisterAsync(string username, string pin)
    {
        // Reject common/weak PINs
        if (WeakPins.Contains(pin))
            throw new ArgumentException("PIN is too common. Please choose a more unique PIN.");

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

    // Validates user login. Returns a LoginResult with success/failure and a message.
    // Also handles account lockout logic via LoginAttemptService.
    public async Task<LoginResult> LoginAsync(string username, string pin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return new LoginResult { Success = false, Message = "Invalid credentials." };

        // Check if the account is currently locked out
        string? lockoutMessage = _loginAttemptService.CheckLockoutStatus(user);
        if (lockoutMessage != null)
            return new LoginResult { Success = false, Message = lockoutMessage };

        // Verify the PIN
        bool pinCorrect = BCrypt.Net.BCrypt.Verify(pin, user.PinHash);

        if (!pinCorrect)
        {
            // Record the failure and get the appropriate message
            string failMessage = await _loginAttemptService.RecordFailedAttemptAsync(user);
            return new LoginResult { Success = false, Message = failMessage };
        }

        // Success — reset any lockout counters
        await _loginAttemptService.RecordSuccessfulLoginAsync(user);
        return new LoginResult { Success = true, Message = "Login successful.", User = user };
    }
}
