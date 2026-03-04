// EasyPass.App/ViewModels/LoginViewModel.cs
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPass.App.Services;

namespace EasyPass.App.ViewModels
{
    // ViewModel for the Login page.
    // Handles login logic, biometric check, and loading/error state.
    // Navigation is handled by the code-behind (LoginPage.xaml.cs).
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authService;
        private readonly IHttpClientFactory _httpClientFactory;

        // Two-way bound to the Username entry field
        [ObservableProperty]
        private string email = "";

        // Two-way bound to the PIN entry field
        [ObservableProperty]
        private string pin = "";

        // Controls ActivityIndicator visibility
        [ObservableProperty]
        private bool isLoading = false;

        // Shows/hides the biometric login button
        [ObservableProperty]
        private bool isBiometricVisible = false;

        // Bound to the error label — also notifies HasError so the label hides/shows
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string errorMessage = "";

        // Used to show/hide the error label in XAML
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // The code-behind subscribes to this event and handles navigation
        public event Action? LoginSucceeded;

        public LoginViewModel(IAuthenticationService authService, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _httpClientFactory = httpClientFactory;
        }

        // Called from code-behind's Loaded event to check biometric availability
        public async Task InitializeAsync()
        {
            try
            {
                bool biometricAvailable = await _authService.IsBiometricAvailableAsync();
                bool hasToken = await _authService.HasValidTokenAsync();

                // Only show biometric button if biometrics are available AND we have a stored token
                IsBiometricVisible = biometricAvailable && hasToken;
            }
            catch
            {
                IsBiometricVisible = false;
            }
        }

        // Called when the Login button is tapped
        // [RelayCommand] generates LoginCommand which auto-disables during execution
        [RelayCommand]
        private async Task LoginAsync()
        {
            // Clear previous error
            ErrorMessage = "";

            // Validate inputs
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Pin))
            {
                ErrorMessage = "Please enter both username and PIN.";
                return;
            }

            IsLoading = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient("EasyPassAuth");
                var loginRequest = new { Username = Email, Pin = Pin };

                var response = await httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Invalid username or PIN.";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Token == null)
                {
                    ErrorMessage = "Failed to receive token.";
                    return;
                }

                // Store the token securely
                await _authService.StoreTokenAsync(result.Token);

                // Tell the code-behind to navigate to PasswordsPage
                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                if (ErrorHelper.IsNetworkError(ex))
                    ErrorMessage = "No internet connection. Please check your network.";
                else
                    ErrorMessage = "Login failed. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Called when the Biometric Login button is tapped
        [RelayCommand]
        private async Task LoginWithBiometricAsync()
        {
            ErrorMessage = "";

            try
            {
                bool isAuthenticated = await _authService.AuthenticateWithBiometricsAsync();
                if (!isAuthenticated)
                {
                    ErrorMessage = "Biometric authentication failed.";
                    return;
                }

                var token = await _authService.GetStoredTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    ErrorMessage = "No stored credentials found.";
                    return;
                }

                // Tell the code-behind to navigate
                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ErrorHelper.GetUserFriendlyMessage(ex);
            }
        }

        // Private class to deserialize the login API response
        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
