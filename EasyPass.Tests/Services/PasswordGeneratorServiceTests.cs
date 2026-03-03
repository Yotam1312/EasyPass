using EasyPass.API.Services;
using Xunit;

namespace EasyPass.Tests.Services
{
    /// <summary>
    /// Unit tests for PasswordGeneratorService.
    /// These tests check password length, character sets, boundary clamping,
    /// and randomness without making any HTTP calls.
    /// </summary>
    public class PasswordGeneratorServiceTests
    {
        private readonly PasswordGeneratorService _passwordGenerator;

        // The exact symbol characters defined in PasswordGeneratorService.
        // Used to verify symbols are included or excluded correctly.
        private static readonly string Symbols = "!@#$%^&*()-_=+<>?";

        // The full set of valid characters the generator can produce.
        // Letters (lower + upper) + digits + symbols.
        private static readonly string ValidChars =
            "abcdefghijklmnopqrstuvwxyz" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "0123456789" +
            "!@#$%^&*()-_=+<>?";

        public PasswordGeneratorServiceTests()
        {
            // PasswordGeneratorService has no constructor dependencies,
            // so we just create a new instance directly.
            _passwordGenerator = new PasswordGeneratorService();
        }

        [Fact]
        public void Generate_DefaultParams_ShouldReturn12Chars()
        {
            // Arrange - Use all default parameters (length=12, useSymbols=true)

            // Act
            string password = _passwordGenerator.Generate();

            // Assert - Default length is 12
            Assert.Equal(12, password.Length);
        }

        [Fact]
        public void Generate_Length20_ShouldReturn20Chars()
        {
            // Arrange - Request a specific length of 20

            // Act
            string password = _passwordGenerator.Generate(length: 20);

            // Assert
            Assert.Equal(20, password.Length);
        }

        [Fact]
        public void Generate_LengthBelowMin_ShouldClampTo4()
        {
            // Arrange - Request length 2, which is below the minimum of 4.
            // The service should clamp it up to 4 instead of crashing.

            // Act
            string password = _passwordGenerator.Generate(length: 2);

            // Assert - Clamped to the minimum allowed length
            Assert.Equal(4, password.Length);
        }

        [Fact]
        public void Generate_LengthAboveMax_ShouldClampTo128()
        {
            // Arrange - Request length 200, which is above the maximum of 128.
            // The service should clamp it down to 128.

            // Act
            string password = _passwordGenerator.Generate(length: 200);

            // Assert - Clamped to the maximum allowed length
            Assert.Equal(128, password.Length);
        }

        [Fact]
        public void Generate_SymbolsFalse_ShouldContainNoSymbols()
        {
            // Arrange - Generate a longer password with symbols turned off.
            // Length 50 gives us a good sample to check.

            // Act
            string password = _passwordGenerator.Generate(length: 50, useSymbols: false);

            // Assert - Loop through every character and make sure it is not a symbol
            foreach (char c in password)
            {
                Assert.False(Symbols.Contains(c), $"Password should not contain symbol '{c}' when useSymbols=false");
            }
        }

        [Fact]
        public void Generate_SymbolsTrue_ShouldOnlyContainValidChars()
        {
            // Arrange - Generate a password with symbols enabled.
            // Every character in the result must come from the known valid set.

            // Act
            string password = _passwordGenerator.Generate(length: 50, useSymbols: true);

            // Assert - Loop through every character and make sure it is in the valid set
            foreach (char c in password)
            {
                Assert.True(ValidChars.Contains(c), $"Password contains unexpected character '{c}'");
            }
        }

        [Fact]
        public void Generate_CalledTwice_ShouldReturnDifferentResults()
        {
            // Arrange - We will call Generate twice with the same parameters.
            // The service uses cryptographic randomness (RandomNumberGenerator),
            // so two calls should produce different passwords.

            // Act
            string password1 = _passwordGenerator.Generate(length: 20);
            string password2 = _passwordGenerator.Generate(length: 20);

            // Assert - The two passwords should not be identical
            Assert.NotEqual(password1, password2);
        }
    }
}
