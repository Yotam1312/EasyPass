using EasyPass.API.Data;
using EasyPass.API.Models;
using Microsoft.Extensions.Logging;

namespace EasyPass.API.Services;

public class LoginAttemptService
{
    private readonly EasyPassContext _context;
    private readonly ILogger<LoginAttemptService> _logger;

    public LoginAttemptService(EasyPassContext context, ILogger<LoginAttemptService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Checks if the user is currently locked out.
    // Returns null if not locked, or an error message string if locked.
    public string? CheckLockoutStatus(User user)
    {
        // Check for permanent lock first
        if (user.IsPermanentlyLocked)
        {
            return "Account is permanently locked due to too many failed attempts. Please contact support.";
        }

        // Check for timed lockout that hasn't expired yet
        if (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow)
        {
            int secondsLeft = (int)(user.LockoutEndAt.Value - DateTime.UtcNow).TotalSeconds;
            return $"Account is temporarily locked. Try again in {secondsLeft} seconds.";
        }

        // Not locked
        return null;
    }

    // Call this after every FAILED login attempt.
    // Updates the user's failure count and lockout state, then saves to DB.
    // Returns a message describing the current state.
    public async Task<string> RecordFailedAttemptAsync(User user)
    {
        // Increment the failure counter
        user.FailedLoginCount++;

        _logger.LogWarning("Failed login attempt for user {Username}. Total consecutive failures: {Count}",
            user.Username, user.FailedLoginCount);

        string message;

        // Decide what happens based on how many failures
        if (user.FailedLoginCount >= 10)
        {
            // Permanent lock after 10 failures
            user.IsPermanentlyLocked = true;
            message = "Too many failed attempts. Account is permanently locked. Please contact support.";
        }
        else if (user.FailedLoginCount >= 7)
        {
            // 5-minute lockout after 7 failures
            user.LockoutEndAt = DateTime.UtcNow.AddMinutes(5);
            message = "Too many failed attempts. Account locked for 5 minutes.";
        }
        else if (user.FailedLoginCount >= 5)
        {
            // 30-second lockout after 5 failures
            user.LockoutEndAt = DateTime.UtcNow.AddSeconds(30);
            message = "Too many failed attempts. Account locked for 30 seconds.";
        }
        else if (user.FailedLoginCount >= 3)
        {
            // Warning message after 3 failures (no lockout yet)
            message = $"Invalid credentials. Warning: {user.FailedLoginCount} failed attempts detected.";
        }
        else
        {
            // Generic message for 1-2 failures
            message = "Invalid credentials.";
        }

        // Save changes to the database
        await _context.SaveChangesAsync();
        return message;
    }

    // Call this after a SUCCESSFUL login.
    // Resets the failure counter and any active timed lockout.
    public async Task RecordSuccessfulLoginAsync(User user)
    {
        user.FailedLoginCount = 0;
        user.LockoutEndAt = null;

        // Save changes to the database
        await _context.SaveChangesAsync();
    }
}
