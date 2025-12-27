using System;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using EasyPass.App.Services;

namespace EasyPass.App.Views;

public partial class RegisterPage : ContentPage
{
    // HttpClient to make API calls
    private readonly HttpClient _httpClient;

    public RegisterPage()
    {
        InitializeComponent();

        // Create HttpClient with the API base address
        _httpClient = new HttpClient(new AuthenticationHandler())
        {
            BaseAddress = new Uri(AppConfig.ApiBaseUrl)
        };
    }

    // Handles the Register button click
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Hide any previous error message
        ErrorLabel.IsVisible = false;

        // Get the values from the input fields
        string email = EmailEntry.Text?.Trim() ?? "";
        string pin = PinEntry.Text?.Trim() ?? "";
        string confirmPin = ConfirmPinEntry.Text?.Trim() ?? "";

        // Validate email is not empty
        if (string.IsNullOrEmpty(email))
        {
            ShowError("Please enter your email.");
            return;
        }

        // Validate email format (simple check for @ symbol)
        if (!email.Contains("@"))
        {
            ShowError("Please enter a valid email address.");
            return;
        }

        // Validate PIN is not empty
        if (string.IsNullOrEmpty(pin))
        {
            ShowError("Please enter a PIN.");
            return;
        }

        // Validate PIN is at least 4 digits
        if (pin.Length < 4)
        {
            ShowError("PIN must be at least 4 digits.");
            return;
        }

        // Validate PIN contains only numbers
        bool isNumericPin = true;
        foreach (char c in pin)
        {
            if (!char.IsDigit(c))
            {
                isNumericPin = false;
                break;
            }
        }
        if (!isNumericPin)
        {
            ShowError("PIN must contain only numbers.");
            return;
        }

        // Validate PINs match
        if (pin != confirmPin)
        {
            ShowError("PINs do not match.");
            return;
        }

        // Show loading indicator and disable register button
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RegisterButton.IsEnabled = false;

        try
        {
            // Create the registration request
            var registerRequest = new
            {
                Username = email,
                Pin = pin
            };

            // Send POST request to the API
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerRequest);

            // Check if registration was successful
            if (response.IsSuccessStatusCode)
            {
                // Show success message
                await DisplayAlert("Success", "Account created successfully! You can now log in.", "OK");

                // Go back to login page
                await Navigation.PopAsync();
            }
            else
            {
                // Read error message from API
                string errorMessage = await response.Content.ReadAsStringAsync();
                ShowError("Registration failed: " + errorMessage);
            }
        }
        catch (Exception ex)
        {
            // Show error if API call failed
            ShowError("Registration failed: " + ex.Message);
        }
        finally
        {
            // Hide loading indicator and enable register button
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RegisterButton.IsEnabled = true;
        }
    }

    // Shows an error message to the user
    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    // Handles the Back to Login button click
    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        // Go back to the login page
        await Navigation.PopAsync();
    }
}
