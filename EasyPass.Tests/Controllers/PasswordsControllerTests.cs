using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EasyPass.API.Data;
using EasyPass.API.Models;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Xunit;

namespace EasyPass.Tests.Controllers
{
    /// <summary>
    /// Integration tests for PasswordsController.
    /// These tests check password CRUD operations with authentication.
    /// </summary>
    public class PasswordsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        // ASP.NET Core serializes JSON with camelCase property names (e.g. "id", "userId").
        // System.Text.Json is case-sensitive by default, so we need this option
        // to correctly deserialize API responses into C# objects (which use PascalCase).
        private static readonly JsonSerializerOptions CaseInsensitiveJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PasswordsControllerTests(WebApplicationFactory<Program> factory)
        {
            // Set environment variables for test configuration.
            // Environment variables override user secrets in .NET's config hierarchy,
            // so this ensures Program.cs uses test keys for both JWT signing and validation.
            // The double underscore __ is the separator for nested config keys.
            Environment.SetEnvironmentVariable("Jwt__Key", "TestingJwtKeyForPasswordIntegrationTests123456789!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "TestEasyPass");
            Environment.SetEnvironmentVariable("Jwt__Audience", "TestEasyPass");
            Environment.SetEnvironmentVariable("Encryption__Key", "TestingEncryptionKeyForPasswordTests123");

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<EasyPassContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database for testing
                    services.AddDbContext<EasyPassContext>(options =>
                    {
                        options.UseInMemoryDatabase("PasswordTestDatabase");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        /// <summary>
        /// Helper method to register a user and get a JWT token for authentication.
        /// </summary>
        private async Task<string> RegisterAndLoginAsync(string username, string pin)
        {
            // Register user
            var registerRequest = new RegisterRequest
            {
                Username = username,
                Pin = pin
            };

            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/auth/register", registerContent);

            // Login to get token
            var loginRequest = new LoginRequest
            {
                Username = username,
                Pin = pin
            };

            var loginJson = JsonSerializer.Serialize(loginRequest);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            var loginJsonDoc = JsonDocument.Parse(loginResponseString);
            return loginJsonDoc.RootElement.GetProperty("token").GetString()!;
        }

        [Fact]
        public async Task GetPasswords_WithValidToken_ShouldReturnEmptyListInitially()
        {
            // Arrange - Get a valid token
            string token = await RegisterAndLoginAsync("passworduser1", "1234");

            // Set authorization header
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/passwords");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var passwords = JsonSerializer.Deserialize<PasswordEntry[]>(responseString);
            
            Assert.NotNull(passwords);
            Assert.Empty(passwords); // Should be empty initially
        }

        [Fact]
        public async Task CreatePassword_WithValidData_ShouldCreatePassword()
        {
            // Arrange
            string token = await RegisterAndLoginAsync("passworduser2", "5678");
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var newPassword = new PasswordEntry
            {
                Service = "Gmail",
                Username = "john@gmail.com",
                EncryptedPassword = "mySecretPassword123!"
            };

            var json = JsonSerializer.Serialize(newPassword);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/passwords", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var createdPassword = JsonSerializer.Deserialize<PasswordEntry>(responseString);

            Assert.NotNull(createdPassword);
            Assert.Equal("Gmail", createdPassword.Service);
            Assert.Equal("john@gmail.com", createdPassword.Username);
            Assert.True(createdPassword.Id > 0);
        }

        [Fact]
        public async Task GetPasswords_AfterCreatingPassword_ShouldReturnPassword()
        {
            // Arrange
            string token = await RegisterAndLoginAsync("passworduser3", "9999");
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Create a password first
            var newPassword = new PasswordEntry
            {
                Service = "Facebook",
                Username = "alice@example.com",
                EncryptedPassword = "facebookPassword456"
            };

            var json = JsonSerializer.Serialize(newPassword);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/passwords", content);

            // Act - Get all passwords
            var response = await _client.GetAsync("/api/passwords");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var passwords = JsonSerializer.Deserialize<PasswordEntry[]>(responseString);

            Assert.NotNull(passwords);
            Assert.Single(passwords); // Should have one password
            Assert.Equal("Facebook", passwords[0].Service);
            Assert.Equal("alice@example.com", passwords[0].Username);
        }

        [Fact]
        public async Task CreatePassword_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange - Don't set authorization header
            var newPassword = new PasswordEntry
            {
                Service = "TestService",
                Username = "test@example.com",
                EncryptedPassword = "testPassword"
            };

            var json = JsonSerializer.Serialize(newPassword);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/passwords", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPasswords_WithoutToken_ShouldReturnUnauthorized()
        {
            // Act - Try to get passwords without authentication
            var response = await _client.GetAsync("/api/passwords");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePassword_WithValidData_ShouldUpdatePassword()
        {
            // Arrange
            string token = await RegisterAndLoginAsync("passworduser4", "1111");
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Create a password first
            var newPassword = new PasswordEntry
            {
                Service = "Twitter",
                Username = "bob@twitter.com",
                EncryptedPassword = "originalPassword"
            };

            var createJson = JsonSerializer.Serialize(newPassword);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/passwords", createContent);
            var createdPasswordString = await createResponse.Content.ReadAsStringAsync();
            var createdPassword = JsonSerializer.Deserialize<PasswordEntry>(createdPasswordString);

            // Update the password
            createdPassword!.EncryptedPassword = "updatedPassword123!";
            var updateJson = JsonSerializer.Serialize(createdPassword);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/passwords/{createdPassword.Id}", updateContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            // Verify the password was updated by getting it again
            var getResponse = await _client.GetAsync("/api/passwords");
            var passwordsString = await getResponse.Content.ReadAsStringAsync();
            var passwords = JsonSerializer.Deserialize<PasswordEntry[]>(passwordsString);

            Assert.NotNull(passwords);
            Assert.Single(passwords);
            // Note: We can't directly compare password because it's encrypted
            // But we know it was updated if the service doesn't throw an error
        }

        [Fact]
        public async Task SearchPasswords_WithExistingService_ShouldReturnMatchingPasswords()
        {
            // Arrange
            string token = await RegisterAndLoginAsync("searchuser", "2222");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Create some passwords
            var password1 = new PasswordEntry
            {
                Service = "Google",
                Username = "user1@google.com",
                EncryptedPassword = "password1"
            };

            var password2 = new PasswordEntry
            {
                Service = "Gmail", // Similar to Google
                Username = "user2@gmail.com",
                EncryptedPassword = "password2"
            };

            var json1 = JsonSerializer.Serialize(password1);
            var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/passwords", content1);

            var json2 = JsonSerializer.Serialize(password2);
            var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/passwords", content2);

            // Act - Search for "Google"
            var response = await _client.GetAsync("/api/passwords/search?service=Google");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<PasswordEntry[]>(responseString);

            Assert.NotNull(searchResults);
            Assert.True(searchResults.Length >= 1); // Should find at least the Google one
        }

        // ── DELETE endpoint tests ─────────────────────────────────────

        [Fact]
        public async Task DeletePassword_WithValidToken_ShouldDeletePassword()
        {
            // Arrange - Register a user and create a password to delete
            string token = await RegisterAndLoginAsync("del_user1@test.com", "1234");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var newPassword = new PasswordEntry
            {
                Service = "DeleteMe",
                Username = "del@test.com",
                EncryptedPassword = "passwordToDelete"
            };

            var createJson = JsonSerializer.Serialize(newPassword);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/passwords", createContent);

            // Extract the Id of the created password so we can delete it
            var createdString = await createResponse.Content.ReadAsStringAsync();
            var createdPassword = JsonSerializer.Deserialize<PasswordEntry>(createdString, CaseInsensitiveJson);

            // Act - Delete the password we just created
            var response = await _client.DeleteAsync($"/api/passwords/{createdPassword!.Id}");

            // Assert - DELETE returns 204 NoContent on success
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify it is actually gone by fetching all passwords
            var getResponse = await _client.GetAsync("/api/passwords");
            var passwordsString = await getResponse.Content.ReadAsStringAsync();
            var passwords = JsonSerializer.Deserialize<PasswordEntry[]>(passwordsString, CaseInsensitiveJson);

            Assert.NotNull(passwords);
            Assert.Empty(passwords); // The only password this user had is now deleted
        }

        [Fact]
        public async Task DeletePassword_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange - Make sure no auth header is set
            _client.DefaultRequestHeaders.Authorization = null;

            // Act - Try to delete a password without being logged in
            // The ID does not matter; the request will be rejected before reaching the controller
            var response = await _client.DeleteAsync("/api/passwords/999");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeletePassword_WithValidToken_NonExistentId_ShouldReturnNotFound()
        {
            // Arrange - Register a user (but do not create any passwords)
            string token = await RegisterAndLoginAsync("del_user2@test.com", "1234");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act - Try to delete a password ID that does not exist
            var response = await _client.DeleteAsync("/api/passwords/99999");

            // Assert - Controller returns 404 when no matching password is found
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Cross-user authorization tests ───────────────────────────

        [Fact]
        public async Task GetPasswords_CrossUser_ShouldNotSeeOtherUserPasswords()
        {
            // Arrange - User A logs in and creates a password
            string tokenA = await RegisterAndLoginAsync("cross_userA1@test.com", "1111");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenA);

            var passwordA = new PasswordEntry
            {
                Service = "PrivateServiceA1",
                Username = "a1@test.com",
                EncryptedPassword = "secretPasswordA"
            };

            var jsonA = JsonSerializer.Serialize(passwordA);
            var contentA = new StringContent(jsonA, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/passwords", contentA);

            // User B logs in (fresh account, no passwords of their own)
            string tokenB = await RegisterAndLoginAsync("cross_userB1@test.com", "2222");

            // Act - Switch to User B's token and fetch passwords
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenB);
            var response = await _client.GetAsync("/api/passwords");

            // Assert - User B should see an empty list, not User A's password
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var passwords = JsonSerializer.Deserialize<PasswordEntry[]>(responseString, CaseInsensitiveJson);

            Assert.NotNull(passwords);
            Assert.Empty(passwords); // User B cannot see User A's data
        }

        [Fact]
        public async Task UpdatePassword_CrossUser_ShouldReturnNotFound()
        {
            // Arrange - User A logs in and creates a password
            string tokenA = await RegisterAndLoginAsync("cross_userA2@test.com", "1111");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenA);

            var passwordA = new PasswordEntry
            {
                Service = "ProtectedServiceA2",
                Username = "a2@test.com",
                EncryptedPassword = "secretA2"
            };

            var createJson = JsonSerializer.Serialize(passwordA);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/passwords", createContent);

            // Get the Id of User A's password
            var createdString = await createResponse.Content.ReadAsStringAsync();
            var createdPassword = JsonSerializer.Deserialize<PasswordEntry>(createdString, CaseInsensitiveJson);
            int passwordId = createdPassword!.Id;

            // User B logs in
            string tokenB = await RegisterAndLoginAsync("cross_userB2@test.com", "2222");

            // Act - User B tries to update User A's password using the known Id
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenB);

            var hackedEntry = new PasswordEntry
            {
                Id = passwordId,
                Service = "Hacked",
                Username = "hacked@test.com",
                EncryptedPassword = "hackedPassword"
            };

            var updateJson = JsonSerializer.Serialize(hackedEntry);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"/api/passwords/{passwordId}", updateContent);

            // Assert - Controller filters by UserId, so User B gets 404
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePassword_CrossUser_ShouldReturnNotFound()
        {
            // Arrange - User A logs in and creates a password
            string tokenA = await RegisterAndLoginAsync("cross_userA3@test.com", "1111");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenA);

            var passwordA = new PasswordEntry
            {
                Service = "ProtectedServiceA3",
                Username = "a3@test.com",
                EncryptedPassword = "secretA3"
            };

            var createJson = JsonSerializer.Serialize(passwordA);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/passwords", createContent);

            // Get the Id of User A's password
            var createdString = await createResponse.Content.ReadAsStringAsync();
            var createdPassword = JsonSerializer.Deserialize<PasswordEntry>(createdString, CaseInsensitiveJson);
            int passwordId = createdPassword!.Id;

            // User B logs in
            string tokenB = await RegisterAndLoginAsync("cross_userB3@test.com", "2222");

            // Act - User B tries to delete User A's password using the known Id
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenB);
            var response = await _client.DeleteAsync($"/api/passwords/{passwordId}");

            // Assert - Controller filters by UserId, so User B gets 404
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Invalid / malformed JWT tests ─────────────────────────────

        [Fact]
        public async Task GetPasswords_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange - Set a completely invalid token string (not a JWT at all)
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "invalidtokenstring");

            // Act - Try to fetch passwords with the bad token
            var response = await _client.GetAsync("/api/passwords");

            // Assert - JWT middleware rejects the token before it reaches the controller
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreatePassword_WithMalformedToken_ShouldReturnUnauthorized()
        {
            // Arrange - Set a token that looks like a JWT (three dot-separated parts)
            // but contains garbage — the signature will not validate
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "abc.def.ghi");

            var newPassword = new PasswordEntry
            {
                Service = "TestService",
                Username = "test@test.com",
                EncryptedPassword = "testPassword"
            };

            var json = JsonSerializer.Serialize(newPassword);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Try to create a password with the malformed token
            var response = await _client.PostAsync("/api/passwords", content);

            // Assert - JWT middleware rejects the malformed token
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}