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
    /// Integration tests for AuthController.
    /// These tests check the whole authentication flow from HTTP requests to database.
    /// </summary>
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
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
                        options.UseInMemoryDatabase("TestDatabase");
                    });

                    // Add test JWT configuration
                    services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(provider =>
                    {
                        var testConfig = new Dictionary<string, string>
                        {
                            {"Jwt:Key", "TestingJwtKeyForIntegrationTests123456789!"},
                            {"Jwt:Issuer", "TestEasyPass"},
                            {"Jwt:Audience", "TestEasyPass"},
                            {"Encryption:Key", "TestingEncryptionKeyForIntegrationTests123"}
                        };

                        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                            .AddInMemoryCollection(testConfig!)
                            .Build();
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Register_WithValidUser_ShouldReturnSuccess()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "integrationtest",
                Pin = "1234"
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("integrationtest", responseString);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange - First register a user
            var registerRequest = new RegisterRequest
            {
                Username = "logintest",
                Pin = "5678"
            };

            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/auth/register", registerContent);

            // Now try to login
            var loginRequest = new LoginRequest
            {
                Username = "logintest",
                Pin = "5678"
            };

            var loginJson = JsonSerializer.Serialize(loginRequest);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", loginContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            
            // Response should contain a token
            Assert.Contains("token", responseString.ToLower());
            
            // Parse the JSON to check structure
            var jsonDoc = JsonDocument.Parse(responseString);
            Assert.True(jsonDoc.RootElement.TryGetProperty("token", out var tokenElement));
            Assert.True(tokenElement.GetString()!.Length > 0);
        }

        [Fact]
        public async Task Login_WithWrongCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "nonexistent",
                Pin = "wrongpin"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateUsername_ShouldReturnBadRequest()
        {
            // Arrange - Register first user
            var registerRequest1 = new RegisterRequest
            {
                Username = "duplicatetest",
                Pin = "1111"
            };

            var json1 = JsonSerializer.Serialize(registerRequest1);
            var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/auth/register", content1);

            // Try to register same username again
            var registerRequest2 = new RegisterRequest
            {
                Username = "duplicatetest",
                Pin = "2222"
            };

            var json2 = JsonSerializer.Serialize(registerRequest2);
            var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content2);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithEmptyUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "",
                Pin = "1234"
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithEmptyPin_ShouldReturnBadRequest()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "emptypintest",
                Pin = ""
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}