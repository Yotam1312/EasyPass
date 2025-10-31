using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace EasyPass.App.Services
{
    public class AuthenticationService
    {
        private const string TOKEN_KEY = "jwt_token";
        private readonly IFingerprint _fingerprint;
        
        public AuthenticationService()
        {
            _fingerprint = CrossFingerprint.Current;
        }

        public async Task<bool> IsBiometricAvailableAsync()
        {
            try
            {
                var availability = await _fingerprint.GetAvailabilityAsync();
                return availability == FingerprintAvailability.Available;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AuthenticateWithBiometricsAsync()
        {
            try
            {
                var request = new AuthenticationRequestConfiguration(
                    "Verify your identity",
                    "Use your fingerprint or face to access EasyPass"
                );

                var result = await _fingerprint.AuthenticateAsync(request);
                return result.Authenticated;
            }
            catch
            {
                return false;
            }
        }

        public async Task StoreTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            await SecureStorage.Default.SetAsync(TOKEN_KEY, token);
        }

        public async Task<string?> GetStoredTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(TOKEN_KEY);
        }

        public async Task ClearTokenAsync()
        {
            SecureStorage.Default.Remove(TOKEN_KEY);
        }

        public async Task<bool> HasValidTokenAsync()
        {
            var token = await GetStoredTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public static void ConfigureHttpClient(HttpClient client, string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}