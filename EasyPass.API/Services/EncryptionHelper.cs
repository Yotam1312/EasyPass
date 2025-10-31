using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EasyPass.API.Services
{
    // AES-256 encryption helper. The key in configuration is derived with SHA-256 to ensure 32 bytes.
    public class EncryptionHelper
    {
        private readonly byte[] _key;

        public EncryptionHelper(IConfiguration config)
        {
            var keyString = config["Encryption:Key"] ?? throw new ArgumentNullException("Encryption:Key is not configured");
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(keyString)); // 32 bytes
        }

        public string Encrypt(string plainText)
        {
            if (plainText == null) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            var iv = aes.IV;
            var cipherBytes = ms.ToArray();

            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var ivLength = aes.BlockSize / 8; // 16 bytes for AES
            if (fullCipher.Length < ivLength) return string.Empty;

            var iv = new byte[ivLength];
            var cipher = new byte[fullCipher.Length - ivLength];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
            Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

            using var ms = new MemoryStream(cipher);
            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }
}
