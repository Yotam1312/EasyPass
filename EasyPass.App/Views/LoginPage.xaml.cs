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

    // Constructor receives services from Dependency Injection
    public LoginPage(IHttpClientFactory httpClientFactory, AuthenticationService authService)
    {
        InitializeComponent();

        // Get the HttpClient for authentication (without /api/ path)
        _httpClient = httpClientFactory.CreateClient("EasyPassAuth");
        _authService = authService;

        // Use Loaded event for safer async initialization
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Unsubscribe to prevent multiple calls
        this.Loaded -= OnPageLoaded;
        await CheckAuthenticationStateAsync();
    }

    private async Task CheckAuthenticationStateAsync()
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
            // Clear the navigation stack so user can't go back to login
            AuthenticationService.ConfigureHttpClient(_httpClient, token);
            // Use App.GetPage to get the page through DI
            Application.Current!.MainPage = new NavigationPage(App.GetPage<PasswordsPage>());
        }
        catch (Exception ex)
        {
            // Show user-friendly error message based on error type
            string message = ErrorHelper.GetUserFriendlyMessage(ex);
            await DisplayAlert("Error", message, "OK");
        }
    }

    // Handles login button click and sends credentials to the API
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Hide any previous error message
        ErrorLabel.IsVisible = false;

        // Get the values from the input fields
        string username = UsernameEntry.Text?.Trim() ?? "";
        string pin = PinEntry.Text?.Trim() ?? "";

        // Check if both fields are filled
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pin))
        {
            ShowError("Please enter both username and PIN.");
            return;
        }

        // Show loading indicator and disable login button
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoginButton.IsEnabled = false;

        try
        {
            // Create the login request
            var loginRequest = new { Username = username, Pin = pin };

            // Send POST request to the API
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

            // Check if login was successful
            if (!response.IsSuccessStatusCode)
            {
                ShowError("Invalid username or PIN.");
                return;
            }

            // Read the response
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token == null)
            {
                ShowError("Failed to receive token.");
                return;
            }

            // Store token securely
            await _authService.StoreTokenAsync(result.Token);
            AuthenticationService.ConfigureHttpClient(_httpClient, result.Token);

            // Navigate to passwords page and clear the navigation stack
            // This prevents the user from pressing back and returning to login
            // Use App.GetPage to get the page through DI
            Application.Current!.MainPage = new NavigationPage(App.GetPage<PasswordsPage>());
        }
        catch (Exception ex)
        {
            // Show user-friendly error message based on error type
            if (ErrorHelper.IsNetworkError(ex))
            {
                ShowError("No internet connection. Please check your network.");
            }
            else
            {
                ShowError("Login failed. Please try again.");
            }
        }
        finally
        {
            // Hide loading indicator and enable login button
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoginButton.IsEnabled = true;
        }
    }

    // Shows an error message to the user
    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
    // Navigates to the RegisterPage when Register button is clicked
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Use App.GetPage to get the page through DI
        await Navigation.PushAsync(App.GetPage<RegisterPage>());
    }


    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}