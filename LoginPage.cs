using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace EasyPass.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5023/api/")
    };

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var pin = PinEntry.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pin))
        {
            await DisplayAlert("Error", "Please enter both username and PIN.", "OK");
            return;
        }

        try
        {
            var loginRequest = new { Username = username, Pin = pin };
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Token is not null)
                {
                    await SecureStorage.Default.SetAsync("jwt_token", result.Token);
                    await DisplayAlert("Success", "Logged in successfully!", "OK");
                    await Navigation.PushAsync(new PasswordsPage());
                }
                else
                {
                    await DisplayAlert("Error", "Invalid response from server.", "OK");
                }
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Login Failed", $"Server returned: {errorMsg}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}