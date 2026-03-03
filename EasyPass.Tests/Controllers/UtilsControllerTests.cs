using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EasyPass.API.Data;
using System.Text.Json;
using Xunit;

namespace EasyPass.Tests.Controllers
{
    /// <summary>
    /// Integration tests for UtilsController.
    /// These tests check the password generation endpoint.
    /// The endpoint is public — no JWT token is needed.
    /// </summary>
    public class UtilsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        // The exact symbol characters used by PasswordGeneratorService.
        // We check against this set when symbols=false to make sure none appear.
        private static readonly string Symbols = "!@#$%^&*()-_=+<>?";

        public UtilsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Override configuration at the builder level so that builder.Configuration
                // (used by Program.cs for JWT middleware setup) picks up test values.
                // Jwt keys are required by Program.cs even though this endpoint
                // does not use authentication.
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var testConfig = new Dictionary<string, string>
                    {
                        {"Jwt:Key", "TestingJwtKeyForUtilsIntegrationTests123456789!"},
                        {"Jwt:Issuer", "TestEasyPass"},
                        {"Jwt:Audience", "TestEasyPass"},
                        {"Encryption:Key", "TestingEncryptionKeyForUtilsTests123456"}
                    };
                    config.AddInMemoryCollection(testConfig!);
                });

                builder.ConfigureServices(services =>
                {
                    // Remove the real database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<EasyPassContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database for testing.
                    // UtilsController does not touch the DB, but Program.cs still
                    // registers it, so we need to replace it to avoid connection errors.
                    services.AddDbContext<EasyPassContext>(options =>
                    {
                        options.UseInMemoryDatabase("UtilsTestDatabase");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GeneratePassword_DefaultParams_ShouldReturnLength12()
        {
            // Arrange - No setup needed, endpoint is public with default params

            // Act - Call the endpoint without any query parameters
            var response = await _client.GetAsync("/api/utils/generate-password");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            // Parse the response: the endpoint returns { "password": "..." }
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            string passwordValue = jsonDoc.RootElement.GetProperty("password").GetString()!;

            // Default length is 12
            Assert.Equal(12, passwordValue.Length);
        }

        [Fact]
        public async Task GeneratePassword_CustomLength20_ShouldReturnLength20()
        {
            // Arrange - We will request a password of length 20

            // Act
            var response = await _client.GetAsync("/api/utils/generate-password?length=20");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            string passwordValue = jsonDoc.RootElement.GetProperty("password").GetString()!;

            Assert.Equal(20, passwordValue.Length);
        }

        [Fact]
        public async Task GeneratePassword_LengthBelowMin_ShouldClampTo4()
        {
            // Arrange - Request a length of 1, which is below the minimum of 4.
            // The service clamps it up to 4.

            // Act
            var response = await _client.GetAsync("/api/utils/generate-password?length=1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            string passwordValue = jsonDoc.RootElement.GetProperty("password").GetString()!;

            // Length 1 is below the minimum, so the service clamps it to 4
            Assert.Equal(4, passwordValue.Length);
        }

        [Fact]
        public async Task GeneratePassword_LengthAboveMax_ShouldClampTo128()
        {
            // Arrange - Request a length of 200, which is above the maximum of 128.
            // The service clamps it down to 128.

            // Act
            var response = await _client.GetAsync("/api/utils/generate-password?length=200");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            string passwordValue = jsonDoc.RootElement.GetProperty("password").GetString()!;

            // Length 200 is above the maximum, so the service clamps it to 128
            Assert.Equal(128, passwordValue.Length);
        }

        [Fact]
        public async Task GeneratePassword_SymbolsFalse_ShouldContainNoSymbols()
        {
            // Arrange - Request a longer password with symbols turned off.
            // We use length=50 so we have a good sample to check against.

            // Act
            var response = await _client.GetAsync("/api/utils/generate-password?length=50&symbols=false");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseString);
            string passwordValue = jsonDoc.RootElement.GetProperty("password").GetString()!;

            // Check every character — none of them should be a symbol
            foreach (char c in passwordValue)
            {
                Assert.False(Symbols.Contains(c), $"Password should not contain symbol '{c}' when symbols=false");
            }
        }

        [Fact]
        public async Task GeneratePassword_CalledTwice_ShouldReturnDifferentPasswords()
        {
            // Arrange - We will call the endpoint twice with the same parameters.
            // Because the generator uses cryptographic randomness, the results
            // should be different each time.

            // Act - First call
            var response1 = await _client.GetAsync("/api/utils/generate-password?length=20");
            var responseString1 = await response1.Content.ReadAsStringAsync();
            var jsonDoc1 = JsonDocument.Parse(responseString1);
            string password1 = jsonDoc1.RootElement.GetProperty("password").GetString()!;

            // Act - Second call (same parameters)
            var response2 = await _client.GetAsync("/api/utils/generate-password?length=20");
            var responseString2 = await response2.Content.ReadAsStringAsync();
            var jsonDoc2 = JsonDocument.Parse(responseString2);
            string password2 = jsonDoc2.RootElement.GetProperty("password").GetString()!;

            // Assert - The two passwords should not be the same
            Assert.NotEqual(password1, password2);
        }
    }
}
