using EasyPass.API.Data;
using EasyPass.API.Models;
using EasyPass.API.Services;
using EasyPass.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EasyPass.Tests.Services
{
    public class LoginAttemptServiceTests : IDisposable
    {
        private readonly LoginAttemptService _service;
        private readonly EasyPassContext _context;

        public LoginAttemptServiceTests()
        {
            string dbName = TestDatabaseHelper.GetUniqueDatabaseName(nameof(LoginAttemptServiceTests));
            _context = TestDatabaseHelper.CreateTestDatabase(dbName);
            // NullLogger is a no-op logger that is safe for unit tests
            _service = new LoginAttemptService(_context, new NullLogger<LoginAttemptService>());
        }

        [Fact]
        public void CheckLockoutStatus_NotLocked_ReturnsNull()
        {
            var user = new User { FailedLoginCount = 0 };
            string? result = _service.CheckLockoutStatus(user);
            Assert.Null(result);
        }

        [Fact]
        public void CheckLockoutStatus_PermanentlyLocked_ReturnsMessage()
        {
            var user = new User { IsPermanentlyLocked = true };
            string? result = _service.CheckLockoutStatus(user);
            Assert.NotNull(result);
            Assert.Contains("permanently locked", result);
        }

        [Fact]
        public void CheckLockoutStatus_TimedLockoutNotExpired_ReturnsMessage()
        {
            var user = new User { LockoutEndAt = DateTime.UtcNow.AddSeconds(20) };
            string? result = _service.CheckLockoutStatus(user);
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckLockoutStatus_TimedLockoutExpired_ReturnsNull()
        {
            // Lockout that ended 10 seconds ago
            var user = new User { LockoutEndAt = DateTime.UtcNow.AddSeconds(-10) };
            string? result = _service.CheckLockoutStatus(user);
            Assert.Null(result);
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_2Failures_ReturnsGenericMessage()
        {
            // Add user to context so SaveChanges works
            var user = new User { Username = "test1@test.com", PinHash = "hash", FailedLoginCount = 1 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string message = await _service.RecordFailedAttemptAsync(user);

            Assert.Equal(2, user.FailedLoginCount);
            Assert.Equal("Invalid credentials.", message);
            Assert.Null(user.LockoutEndAt);
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_3Failures_ReturnsWarning()
        {
            var user = new User { Username = "test2@test.com", PinHash = "hash", FailedLoginCount = 2 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string message = await _service.RecordFailedAttemptAsync(user);

            Assert.Equal(3, user.FailedLoginCount);
            Assert.Contains("Warning", message);
            Assert.Null(user.LockoutEndAt);
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_5Failures_Sets30SecLockout()
        {
            var user = new User { Username = "test3@test.com", PinHash = "hash", FailedLoginCount = 4 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RecordFailedAttemptAsync(user);

            Assert.Equal(5, user.FailedLoginCount);
            Assert.NotNull(user.LockoutEndAt);
            // Should be locked for about 30 seconds
            Assert.True(user.LockoutEndAt > DateTime.UtcNow.AddSeconds(25));
            Assert.True(user.LockoutEndAt < DateTime.UtcNow.AddSeconds(35));
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_7Failures_Sets5MinLockout()
        {
            var user = new User { Username = "test4@test.com", PinHash = "hash", FailedLoginCount = 6 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RecordFailedAttemptAsync(user);

            Assert.Equal(7, user.FailedLoginCount);
            Assert.NotNull(user.LockoutEndAt);
            // Should be locked for about 5 minutes
            Assert.True(user.LockoutEndAt > DateTime.UtcNow.AddMinutes(4));
        }

        [Fact]
        public async Task RecordFailedAttemptAsync_10Failures_SetsPermanentLock()
        {
            var user = new User { Username = "test5@test.com", PinHash = "hash", FailedLoginCount = 9 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RecordFailedAttemptAsync(user);

            Assert.Equal(10, user.FailedLoginCount);
            Assert.True(user.IsPermanentlyLocked);
        }

        [Fact]
        public async Task RecordSuccessfulLoginAsync_ResetsCountAndLockout()
        {
            var user = new User
            {
                Username = "test6@test.com",
                PinHash = "hash",
                FailedLoginCount = 4,
                LockoutEndAt = DateTime.UtcNow.AddSeconds(30)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.RecordSuccessfulLoginAsync(user);

            Assert.Equal(0, user.FailedLoginCount);
            Assert.Null(user.LockoutEndAt);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
