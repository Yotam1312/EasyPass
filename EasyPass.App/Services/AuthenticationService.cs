using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
#if ANDROID || IOS || MACCATALYST
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
#endif

namespace EasyPass.App.Services
{
    public class AuthenticationService
    {
        private const string TOKEN_KEY = "jwt_token";
#if ANDROID || IOS || MACCATALYST
        private readonly IFingerprint? _fingerprint;
#endif
        private readonly bool _isFingerprintSupported;

        public AuthenticationService()
        {
#if ANDROID || IOS || MACCATALYST
            try
            {
                _fingerprint = CrossFingerprint.Current;
                _isFingerprintSupported = true;
            }
            catch
            {
                _fingerprint = null;
                _isFingerprintSupported = false;
            }
#else
            _isFingerprintSupported = false;
#endif
        }

        public async Task<bool> IsBiometricAvailableAsync()
        {
#if ANDROID || IOS || MACCATALYST
            if (!_isFingerprintSupported || _fingerprint == null)
                return false;

            try
            {
                var availability = await _fingerprint.GetAvailabilityAsync();
                return availability == FingerprintAvailability.Available;
            }
            catch
            {
                return false;
            }
#else
            return await Task.FromResult(false);
#endif
        }

        public async Task<bool> AuthenticateWithBiometricsAsync()
        {
#if ANDROID || IOS || MACCATALYST
            if (!_isFingerprintSupported || _fingerprint == null)
                return false;

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
#else
            return await Task.FromResult(false);
#endif
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