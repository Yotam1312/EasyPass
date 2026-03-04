namespace EasyPass.App.Services
{
    // Interface that defines what the authentication service can do.
    // Using an interface lets us swap implementations (e.g., for testing).
    public interface IAuthenticationService
    {
        Task<bool> IsBiometricAvailableAsync();
        Task<bool> AuthenticateWithBiometricsAsync();
        Task StoreTokenAsync(string token);
        Task<string?> GetStoredTokenAsync();
        Task ClearTokenAsync();
        Task<bool> HasValidTokenAsync();
    }
}
