using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace EasyPass.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient = new();

    public LoginPage()
    {
        InitializeComponent();
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

            // Send login request to the API
            var response = await _httpClient.PostAsJsonAsync("http://localhost:5023/api/auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Login Failed", "Invalid username or PIN.", "OK");
                return;
            }

            // Read JWT token from the response
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result?.Token == null)
            {
                await DisplayAlert("Error", "Failed to receive token.", "OK");
                return;
            }

            // Save the token securely
            await SecureStorage.Default.SetAsync("jwt_token", result.Token);

            await DisplayAlert("Success", "Logged in successfully!", "OK");

            // Navigate to the passwords page after successful login
            await Navigation.PushAsync(new PasswordsPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
        }
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
