using System.Security.Cryptography;
using System.Text;

namespace EasyPass.API.Services
{
    public class PasswordGeneratorService
    {
        // Character sets for password generation
        private static readonly string Letters = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string Digits = "0123456789";
        private static readonly string Symbols = "!@#$%^&*()-_=+<>?";

        /// <summary>
        /// Generates a cryptographically secure random password.
        /// Uses RandomNumberGenerator instead of Random for security.
        /// </summary>
        /// <param name="length">Password length (default 12, min 4, max 128)</param>
        /// <param name="useSymbols">Include special characters</param>
        /// <returns>A secure random password string</returns>
        public string Generate(int length = 12, bool useSymbols = true)
        {
            // Validate length to prevent abuse
            if (length < 4)
            {
                length = 4;
            }
            if (length > 128)
            {
                length = 128;
            }

            // Build the character set
            string allChars = Letters + UppercaseLetters + Digits;
            if (useSymbols)
            {
                allChars = allChars + Symbols;
            }

            // Use StringBuilder to build the password
            StringBuilder password = new StringBuilder();

            // Generate each character using cryptographically secure random
            for (int i = 0; i < length; i++)
            {
                // Get a random index within the character set
                int randomIndex = RandomNumberGenerator.GetInt32(allChars.Length);
                password.Append(allChars[randomIndex]);
            }

            return password.ToString();
        }
    }
}
