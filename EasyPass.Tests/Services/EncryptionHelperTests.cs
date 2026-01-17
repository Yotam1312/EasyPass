using Microsoft.Extensions.Configuration;
using EasyPass.API.Services;
using Xunit;

namespace EasyPass.Tests.Services
{
    /// <summary>
    /// Tests for the EncryptionHelper service.
    /// This class tests AES-256 encryption and decryption functionality.
    /// </summary>
    public class EncryptionHelperTests
    {
        private readonly EncryptionHelper _encryptionHelper;

        public EncryptionHelperTests()
        {
            // Create a simple configuration for testing
            // We use a test key - never use this in production!
            var testConfig = new Dictionary<string, string>
            {
                {"Encryption:Key", "TestKey123ForEncryptionTesting456789"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(testConfig!)
                .Build();

            _encryptionHelper = new EncryptionHelper(configuration);
        }

        [Fact]
        public void Encrypt_WithSimpleText_ShouldReturnEncryptedString()
        {
            // Arrange - Set up test data
            string plainText = "Hello, World!";

            // Act - Call the method we're testing
            string result = _encryptionHelper.Encrypt(plainText);

            // Assert - Check the result is what we expect
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.NotEqual(plainText, result); // Encrypted text should be different from original
        }

        [Fact]
        public void Encrypt_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string plainText = "";

            // Act
            string result = _encryptionHelper.Encrypt(plainText);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Encrypt_WithNullString_ShouldReturnEmptyString()
        {
            // Arrange
            string? plainText = null;

            // Act
            string result = _encryptionHelper.Encrypt(plainText!);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Decrypt_WithValidEncryptedText_ShouldReturnOriginalText()
        {
            // Arrange
            string originalText = "MySecretPassword123!";

            // First encrypt the text
            string encryptedText = _encryptionHelper.Encrypt(originalText);

            // Act - Now decrypt it
            string decryptedText = _encryptionHelper.Decrypt(encryptedText);

            // Assert
            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        public void Encrypt_SameTextTwice_ShouldReturnDifferentResults()
        {
            // This tests that each encryption uses a different IV (Initialization Vector)
            // Arrange
            string plainText = "SamePassword123";

            // Act - Encrypt the same text twice
            string firstEncryption = _encryptionHelper.Encrypt(plainText);
            string secondEncryption = _encryptionHelper.Encrypt(plainText);

            // Assert - Results should be different due to random IV
            Assert.NotEqual(firstEncryption, secondEncryption);

            // But both should decrypt to the same original text
            string firstDecrypted = _encryptionHelper.Decrypt(firstEncryption);
            string secondDecrypted = _encryptionHelper.Decrypt(secondEncryption);
            
            Assert.Equal(plainText, firstDecrypted);
            Assert.Equal(plainText, secondDecrypted);
        }

        [Fact]
        public void Encrypt_WithLongPassword_ShouldWorkCorrectly()
        {
            // Test with a longer password like users might have
            // Arrange
            string longPassword = "ThisIsAVeryLongPasswordWithSpecialCharacters!@#$%^&*()1234567890";

            // Act
            string encrypted = _encryptionHelper.Encrypt(longPassword);
            string decrypted = _encryptionHelper.Decrypt(encrypted);

            // Assert
            Assert.Equal(longPassword, decrypted);
        }

        [Fact]
        public void Encrypt_WithSpecialCharacters_ShouldWorkCorrectly()
        {
            // Test with special characters users might use in passwords
            // Arrange
            string specialPassword = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            string encrypted = _encryptionHelper.Encrypt(specialPassword);
            string decrypted = _encryptionHelper.Decrypt(encrypted);

            // Assert
            Assert.Equal(specialPassword, decrypted);
        }

        [Fact]
        public void Decrypt_WithInvalidText_ShouldThrowException()
        {
            // Arrange
            string invalidEncryptedText = "ThisIsNotValidEncryptedText";

            // Act & Assert - We expect this to throw an exception
            Assert.Throws<System.Exception>(() =>
            {
                _encryptionHelper.Decrypt(invalidEncryptedText);
            });
        }
    }
}