using Microsoft.Extensions.Configuration;
using EasyPass.API.Services;
using EasyPass.API.Models;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EasyPass.Tests.Services
{
    /// <summary>
    /// Tests for the JwtService class.
    /// This tests JWT token creation and validation functionality.
    /// </summary>
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;

        public JwtServiceTests()
        {
            // Create test configuration with JWT settings
            var testConfig = new Dictionary<string, string>
            {
                {"Jwt:Key", "ThisIsATestKeyForJwtTokenTesting123456789!"},
                {"Jwt:Issuer", "TestEasyPass"},
                {"Jwt:Audience", "TestEasyPass"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(testConfig!)
                .Build();

            _jwtService = new JwtService(_configuration);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldReturnValidToken()
        {
            // Arrange - Create a test user
            var testUser = new User
            {
                Id = 1,
                Username = "testuser"
            };

            // Act - Generate a token
            string token = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert - Check the token is not empty
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldCreateValidJwtStructure()
        {
            // Arrange
            var testUser = new User
            {
                Id = 42,
                Username = "johndoe"
            };

            // Act
            string token = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert - Check JWT structure (should have 3 parts separated by dots)
            string[] tokenParts = token.Split('.');
            Assert.Equal(3, tokenParts.Length);

            // Each part should not be empty
            Assert.NotEmpty(tokenParts[0]); // Header
            Assert.NotEmpty(tokenParts[1]); // Payload
            Assert.NotEmpty(tokenParts[2]); // Signature
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldContainCorrectClaims()
        {
            // Arrange
            var testUser = new User
            {
                Id = 123,
                Username = "alice"
            };

            // Act
            string token = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert - Parse the token and check claims
            var jwtHandler = new JwtSecurityTokenHandler();
            var jsonToken = jwtHandler.ReadJwtToken(token);

            // Check if the token contains the user ID
            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            Assert.NotNull(userIdClaim);
            Assert.Equal("123", userIdClaim.Value);

            // Check if the token contains the username
            var usernameClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
            Assert.NotNull(usernameClaim);
            Assert.Equal("alice", usernameClaim.Value);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldContainCorrectIssuerAndAudience()
        {
            // Arrange
            var testUser = new User
            {
                Id = 456,
                Username = "bob"
            };

            // Act
            string token = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert - Parse the token and check issuer/audience
            var jwtHandler = new JwtSecurityTokenHandler();
            var jsonToken = jwtHandler.ReadJwtToken(token);

            Assert.Equal("TestEasyPass", jsonToken.Issuer);
            Assert.Contains("TestEasyPass", jsonToken.Audiences);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldHaveExpiryTime()
        {
            // Arrange
            var testUser = new User
            {
                Id = 789,
                Username = "charlie"
            };

            // Act
            string token = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert - Check that token has expiry time
            var jwtHandler = new JwtSecurityTokenHandler();
            var jsonToken = jwtHandler.ReadJwtToken(token);

            // Token should expire sometime in the future
            Assert.True(jsonToken.ValidTo > DateTime.UtcNow);

            // Token should expire within 2 hours (our service sets 1 hour)
            Assert.True(jsonToken.ValidTo <= DateTime.UtcNow.AddHours(2));
        }

        [Fact]
        public void GenerateToken_ForDifferentUsers_ShouldReturnDifferentTokens()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "user1" };
            var user2 = new User { Id = 2, Username = "user2" };

            // Act
            string token1 = _jwtService.GenerateToken(user1.Id, user1.Username);
            string token2 = _jwtService.GenerateToken(user2.Id, user2.Username);

            // Assert
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void GenerateToken_CalledMultipleTimes_ShouldReturnDifferentTokens()
        {
            // Even for the same user, tokens should be different due to different issued times
            // Arrange
            var testUser = new User { Id = 1, Username = "testuser" };

            // Act - Generate tokens with a small delay
            string token1 = _jwtService.GenerateToken(testUser.Id, testUser.Username);
            
            // Wait a tiny bit to ensure different timestamp
            Thread.Sleep(1000);
            
            string token2 = _jwtService.GenerateToken(testUser.Id, testUser.Username);

            // Assert
            Assert.NotEqual(token1, token2);
        }
    }
}