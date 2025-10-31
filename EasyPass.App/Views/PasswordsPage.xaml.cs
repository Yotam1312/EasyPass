using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using EasyPass.App.Models;
using EasyPass.App.Services;

namespace EasyPass.App.Views;

public partial class PasswordsPage : ContentPage
{
    private readonly HttpClient _client;
    private readonly AuthenticationService _authService;
    private List<PasswordEntry> _allPasswords = new();
    private IDispatcherTimer? _toastTimer;

    public PasswordsPage()
    {
        InitializeComponent();
        _authService = new AuthenticationService();
        _client = new HttpClient(new AuthenticationHandler())
        {
            BaseAddress = new Uri("https://easypass-api-plg8.onrender.com/api/")
        };
        
        CheckAuthAndLoadPasswords();
    }

    private async void CheckAuthAndLoadPasswords()
    {
        try
        {
            if (await _authService.IsBiometricAvailableAsync())
            {
                var isAuthenticated = await _authService.AuthenticateWithBiometricsAsync();
                if (!isAuthenticated)
                {
                    await Navigation.PopAsync();
                    return;
                }
            }

            await LoadPasswords();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Authentication failed: {ex.Message}", "OK");
            await Navigation.PopAsync();
        }
    }

    // Loads all passwords for the authenticated user
    public async Task LoadPasswords()
    {
        try
        {
            var token = await _authService.GetStoredTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "User is not authenticated.", "OK");
                await Navigation.PopAsync();
                return;
            }

            var passwords = await _client.GetFromJsonAsync<List<PasswordEntry>>("Passwords");
            _allPasswords = passwords ?? new List<PasswordEntry>();
            PasswordsList.ItemsSource = _allPasswords;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load passwords: {ex.Message}", "OK");
        }
    }

    // Opens a popup to create and save a new password entry
    private async void AddPasswordPopup()
    {
        string service = string.Empty;
        string username = string.Empty;
        string password = string.Empty;
        bool isSaved = false;

        while (!isSaved)
        {
            var serviceInput = await DisplayPromptAsync("Add Password", "Service:", initialValue: service);
            if (serviceInput == null) return;
            service = serviceInput;

            var usernameInput = await DisplayPromptAsync("Add Password", "Username:", initialValue: username);
            if (usernameInput == null) return;
            username = usernameInput;

            string[] actions = { "Type Password", "Generate Strong Password" };
            var action = await DisplayActionSheet("Password", "Cancel", null, actions);
            if (action == "Cancel") return;

            if (action == "Generate Strong Password")
            {
                try
                {
                    var generated = await _client.GetFromJsonAsync<PasswordResponse>("Utils/generate-password");
                    password = generated?.Password ?? string.Empty;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to generate password: {ex.Message}", "OK");
                    continue;
                }
            }
            else
            {
                var passwordInput = await DisplayPromptAsync("Add Password", "Password:", initialValue: password);
                if (passwordInput == null) return;
                password = passwordInput;
            }

            var confirm = await DisplayAlert("Save Password", $"Service: {service}\nUsername: {username}\nPassword: {password}", "Save", "Edit");
            if (!confirm) continue;

            try
            {
                var newPassword = new PasswordEntry
                {
                    Service = service,
                    Username = username,
                    EncryptedPassword = password
                };

                var response = await _client.PostAsJsonAsync("Passwords", newPassword);
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Password saved!", "OK");
                    await LoadPasswords();
                    isSaved = true;
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Failed to save password: {errorMsg}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save password: {ex.Message}", "OK");
            }
        }
    }

    private async void OnAddPasswordClicked(object sender, EventArgs e)
    {
        AddPasswordPopup();
    }

    // Filters password list based on user input in search bar
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            PasswordsList.ItemsSource = _allPasswords;
            return;
        }

        var filtered = _allPasswords.Where(p =>
            (!string.IsNullOrEmpty(p.Service) && p.Service.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(p.Username) && p.Username.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();
        PasswordsList.ItemsSource = filtered;
    }

    // Generates a strong password using the API
    private async void OnGeneratePasswordClicked(object sender, EventArgs e)
    {
        try
        {
            var generated = await _client.GetFromJsonAsync<PasswordResponse>("Utils/generate-password");
            await DisplayAlert("Strong Password Generated", generated?.Password ?? "", "Copy");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to generate password: {ex.Message}", "OK");
        }
    }

    // Deletes a password entry after confirmation
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var id = (int)button.CommandParameter;

        // Find the password entry for a more informative confirmation message
        var entry = _allPasswords.FirstOrDefault(p => p.Id == id);
        if (entry == null) return;

        bool confirm = await DisplayAlert(
            "Delete Password",
            $"Are you sure you want to delete the password for {entry.Service}?\n\nThis action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            var response = await _client.DeleteAsync($"Passwords/{id}");
            if (response.IsSuccessStatusCode)
            {
                ShowToast("Password deleted successfully");
                await LoadPasswords();
            }
            else
            {
                await DisplayAlert("Error", "Failed to delete password.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete password: {ex.Message}", "OK");
        }
    }

    private async void OnMyPasswordsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AllPasswordsPage(_allPasswords));
    }

    // Edits an existing password entry and updates it through the API
    private async void OnEditPasswordClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        if (button.CommandParameter is not int id)
            return;

        var passwordEntry = _allPasswords.FirstOrDefault(p => p.Id == id);
        if (passwordEntry == null)
        {
            await DisplayAlert("Error", "Password entry not found.", "OK");
            return;
        }

        string newService = await DisplayPromptAsync("Edit Password", "Service:", initialValue: passwordEntry.Service) ?? passwordEntry.Service;
        string newUsername = await DisplayPromptAsync("Edit Password", "Username:", initialValue: passwordEntry.Username) ?? passwordEntry.Username;
        string newPassword = await DisplayPromptAsync("Edit Password", "Password:", initialValue: passwordEntry.EncryptedPassword) ?? passwordEntry.EncryptedPassword;

        try
        {
            var updatedEntry = new PasswordEntry
            {
                Id = passwordEntry.Id,
                Service = newService,
                Username = newUsername,
                EncryptedPassword = newPassword,
                UserId = passwordEntry.UserId
            };

            var response = await _client.PutAsJsonAsync($"Passwords/{passwordEntry.Id}", updatedEntry);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Password updated!", "OK");
                await LoadPasswords();
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Failed to update password: {errorMsg}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update password: {ex.Message}", "OK");
        }
    }

    private async void OnCopyPasswordClicked(object sender, EventArgs e)
    {
        try
        {
            var button = (Button)sender;
            var passwordEntry = (PasswordEntry)button.CommandParameter;

            await Clipboard.SetTextAsync(passwordEntry.EncryptedPassword);
            ShowToast("Password copied to clipboard");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to copy password", "OK");
        }
    }

    private void ShowToast(string message)
    {
        // Create and show the toast message
        var toast = new Label
        {
            Text = message,
            TextColor = Colors.White,
            BackgroundColor = Colors.DarkGray,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(16),
            FontSize = 16,
            Opacity = 0
        };

        // Add the toast to the page
        var mainGrid = (Grid)Content;
        mainGrid.Add(toast);
        toast.SetValue(Grid.RowSpanProperty, 2); // Span both rows

        // Center the toast
        toast.VerticalOptions = LayoutOptions.End;
        toast.Margin = new Thickness(32, 0, 32, 32);

        // Animate the toast
        toast.FadeTo(1, 200);

        // Set up timer to remove the toast
        _toastTimer?.Stop();
        _toastTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_toastTimer != null)
        {
            _toastTimer.Interval = TimeSpan.FromSeconds(2);
            _toastTimer.Tick += async (s, e) =>
            {
                await toast.FadeTo(0, 200);
                mainGrid.Remove(toast);
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        }
    }

    private class PasswordResponse
    {
        public string Password { get; set; } = string.Empty;
    }
}
