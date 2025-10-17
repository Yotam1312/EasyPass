using System.Text;

namespace EasyPass.API.Services
{
    public class PasswordGeneratorService
    {
        private static readonly string Letters = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string Digits = "0123456789";
        private static readonly string Symbols = "!@#$%^&*()-_=+<>?";

        private readonly Random _random = new();

        public string Generate(int length = 12, bool useSymbols = true)
        {
            var allChars = Letters + UppercaseLetters + Digits + (useSymbols ? Symbols : "");
            var sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                var idx = _random.Next(allChars.Length);
                sb.Append(allChars[idx]);
            }

            return sb.ToString();
        }
    }
}
