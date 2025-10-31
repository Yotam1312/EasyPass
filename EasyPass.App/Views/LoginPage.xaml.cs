using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using EasyPass.App.Services;
using System.Diagnostics;

namespace EasyPass.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationService _authService;
    private bool _isBiometricAvailable;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthenticationService();
        _httpClient = new HttpClient(new AuthenticationHandler())
        {
            BaseAddress = new Uri("https://easypass-api-plg8.onrender.com/")
        };
        
        // Check for stored credentials and biometric availability
        CheckAuthenticationStateAsync();
    }

    private async void CheckAuthenticationStateAsync()
    {
        try
        {
            _isBiometricAvailable = await _authService.IsBiometricAvailableAsync();
            var hasToken = await _authService.HasValidTokenAsync();

            if (hasToken && _isBiometricAvailable)
            {
                BiometricLoginButton.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking auth state: {ex.Message}");
            BiometricLoginButton.IsVisible = false;
        }
    }

    private async void OnBiometricLoginClicked(object sender, EventArgs e)
    {
        if (!_isBiometricAvailable)
        {
            await DisplayAlert("Error", "Biometric authentication is not available", "OK");
            return;
        }

        try
        {
            var isAuthenticated = await _authService.AuthenticateWithBiometricsAsync();
            if (!isAuthenticated)
            {
                await DisplayAlert("Authentication Failed", "Biometric authentication failed", "OK");
                return;
            }

            var token = await _authService.GetStoredTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "No stored credentials found", "OK");
                return;
            }

            // Configure client and navigate
            AuthenticationService.ConfigureHttpClient(_httpClient, token);
            await Navigation.PushAsync(new PasswordsPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", 
                $"Biometric authentication error: {ex.Message}", "OK");
        }
    }

    // Handles login button click and sends credentials to the API
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string pin = PinEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pin))
        {
            await DisplayAlert("Error", "Please enter both username and PIN.", "OK");
            return;
        }

        try
        {
            var loginRequest = new { Username = username, Pin = pin };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Login Failed", "Invalid username or PIN.", "OK");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
            {
                await DisplayAlert("Error", "Failed to receive token.", "OK");
                return;
            }

            // Store token securely
            await _authService.StoreTokenAsync(result.Token);
            AuthenticationService.ConfigureHttpClient(_httpClient, result.Token);

            await DisplayAlert("Success", "Logged in successfully!", "OK");
            await Navigation.PushAsync(new PasswordsPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }

    }
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = await DisplayPromptAsync("Register", "Enter a username:");
        if (string.IsNullOrWhiteSpace(username))
            return;

        string pin = await DisplayPromptAsync("Register", "Enter a digit PIN:");
        if (string.IsNullOrWhiteSpace(pin))
            return;

        try
        {
            var registerRequest = new { Username = username, Pin = pin };
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register", registerRequest);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", 
                    "Account created successfully! You can now log in.", "OK");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Registration failed: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
        }
    }


    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}