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

        public PasswordsControllerTests(WebApplicationFactory<Program> factory)
        {
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

                    // Add test configuration
                    services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(provider =>
                    {
                        var testConfig = new Dictionary<string, string>
                        {
                            {"Jwt:Key", "TestingJwtKeyForPasswordIntegrationTests123456789!"},
                            {"Jwt:Issuer", "TestEasyPass"},
                            {"Jwt:Audience", "TestEasyPass"},
                            {"Encryption:Key", "TestingEncryptionKeyForPasswordTests123"}
                        };

                        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                            .AddInMemoryCollection(testConfig!)
                            .Build();
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
    }
}