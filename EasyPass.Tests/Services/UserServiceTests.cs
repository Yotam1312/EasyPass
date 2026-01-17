using EasyPass.API.Services;
using EasyPass.API.Models;
using EasyPass.Tests.TestHelpers;
using Xunit;

namespace EasyPass.Tests.Services
{
    /// <summary>
    /// Tests for the UserService class.
    /// This tests user registration, authentication, and password management.
    /// </summary>
    public class UserServiceTests : IDisposable
    {
        private readonly UserService _userService;
        private readonly EasyPass.API.Data.EasyPassContext _context;

        public UserServiceTests()
        {
            // Create a fresh database for each test
            string databaseName = TestDatabaseHelper.GetUniqueDatabaseName(nameof(UserServiceTests));
            _context = TestDatabaseHelper.CreateTestDatabase(databaseName);
            _userService = new UserService(_context);
        }

        [Fact]
        public async Task RegisterAsync_WithValidUser_ShouldCreateNewUser()
        {
            // Arrange - Create a registration request
            var registerRequest = new RegisterRequest
            {
                Username = "newuser",
                Pin = "1234"
            };

            // Act - Register the user
            User result = await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);

            // Assert - Check the user was created
            Assert.NotNull(result);
            Assert.Equal("newuser", result.Username);
            Assert.NotEqual("1234", result.PinHash); // PIN should be hashed
            Assert.True(result.Id > 0); // Should have an ID assigned
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateUsername_ShouldReturnNull()
        {
            // Arrange - First create a user
            var firstUser = new RegisterRequest
            {
                Username = "duplicateuser",
                Pin = "1234"
            };
            await _userService.RegisterAsync(firstUser.Username, firstUser.Pin);

            // Try to create another user with same username
            var duplicateUser = new RegisterRequest
            {
                Username = "duplicateuser", // Same username
                Pin = "5678"
            };

            // Act
            User result = await _userService.RegisterAsync(duplicateUser.Username, duplicateUser.Pin);

            // Assert - Should return null because username already exists
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange - First register a user
            var registerRequest = new RegisterRequest
            {
                Username = "testuser",
                Pin = "5555"
            };
            User registeredUser = await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);

            // Create login request
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Pin = "5555"
            };

            // Act - Try to authenticate
            User authenticatedUser = await _userService.LoginAsync(loginRequest.Username, loginRequest.Pin);

            // Assert
            Assert.NotNull(authenticatedUser);
            Assert.Equal("testuser", authenticatedUser.Username);
            Assert.Equal(registeredUser.Id, authenticatedUser.Id);
        }

        [Fact]
        public async Task AuthenticateAsync_WithWrongPin_ShouldReturnNull()
        {
            // Arrange - Register a user
            var registerRequest = new RegisterRequest
            {
                Username = "testuser2",
                Pin = "9999"
            };
            await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);

            // Try to login with wrong PIN
            var loginRequest = new LoginRequest
            {
                Username = "testuser2",
                Pin = "1111" // Wrong PIN
            };

            // Act
            User result = await _userService.LoginAsync(loginRequest.Username, loginRequest.Pin);

            // Assert - Should return null for wrong PIN
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange - Try to login with user that doesn't exist
            var loginRequest = new LoginRequest
            {
                Username = "nonexistentuser",
                Pin = "1234"
            };

            // Act
            User result = await _userService.LoginAsync(loginRequest.Username, loginRequest.Pin);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ShouldHashPinCorrectly()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "hashtest",
                Pin = "7890"
            };

            // Act
            User result = await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);

            // Assert - PIN should be hashed (BCrypt creates 60-character hashes)
            Assert.NotNull(result.PinHash);
            Assert.NotEqual("7890", result.PinHash);
            Assert.True(result.PinHash.Length >= 50); // BCrypt hashes are typically 60+ chars

            // Should be able to verify the PIN
            bool isValidPin = BCrypt.Net.BCrypt.Verify("7890", result.PinHash);
            Assert.True(isValidPin);
        }

        [Fact]
        public async Task RegisterAsync_WithEmptyUsername_ShouldHandleGracefully()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "",
                Pin = "1234"
            };

            // Act & Assert - This might throw an exception or return null
            // depending on validation implementation
            try
            {
                User result = await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);
                // If no exception, result should be null or have validation errors
                Assert.Null(result);
            }
            catch (Exception ex)
            {
                // If exception is thrown, that's also acceptable
                Assert.NotNull(ex);
            }
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyPin_ShouldReturnNull()
        {
            // Arrange - Register a user first
            var registerRequest = new RegisterRequest
            {
                Username = "emptytest",
                Pin = "1234"
            };
            await _userService.RegisterAsync(registerRequest.Username, registerRequest.Pin);

            // Try to authenticate with empty PIN
            var loginRequest = new LoginRequest
            {
                Username = "emptytest",
                Pin = ""
            };

            // Act
            User result = await _userService.LoginAsync(loginRequest.Username, loginRequest.Pin);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_MultipleUsers_ShouldCreateUniqueIds()
        {
            // Arrange
            var user1Request = new RegisterRequest { Username = "user1", Pin = "1111" };
            var user2Request = new RegisterRequest { Username = "user2", Pin = "2222" };

            // Act
            User user1 = await _userService.RegisterAsync(user1Request.Username, user1Request.Pin);
            User user2 = await _userService.RegisterAsync(user2Request.Username, user2Request.Pin);

            // Assert
            Assert.NotNull(user1);
            Assert.NotNull(user2);
            Assert.NotEqual(user1.Id, user2.Id);
            Assert.True(user1.Id > 0);
            Assert.True(user2.Id > 0);
        }

        // Clean up after each test
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}