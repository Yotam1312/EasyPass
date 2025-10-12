using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using EasyPass.App.Models;

namespace EasyPass.App.Views;

public partial class PasswordsPage : ContentPage
{
    private List<PasswordEntry> _allPasswords = new();

    public PasswordsPage()
    {
        InitializeComponent();
        _ = LoadPasswords();
    }

    // Loads all passwords for the authenticated user
    public async Task LoadPasswords()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "User is not authenticated.", "OK");
                return;
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5023/api/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var passwords = await client.GetFromJsonAsync<List<PasswordEntry>>("Passwords");
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
                    using var client = new HttpClient();
                    var generated = await client.GetFromJsonAsync<PasswordResponse>("http://localhost:5023/api/Utils/generate-password");
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
                var token = await SecureStorage.Default.GetAsync("jwt_token");
                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Error", "User is not authenticated.", "OK");
                    return;
                }

                using var client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:5023/api/");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var newPassword = new PasswordEntry
                {
                    Service = service,
                    Username = username,
                    EncryptedPassword = password
                };

                var response = await client.PostAsJsonAsync("Passwords", newPassword);
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
            using var client = new HttpClient();
            var generated = await client.GetFromJsonAsync<PasswordResponse>("http://localhost:5023/api/Utils/generate-password");
            await DisplayAlert("Strong Password Generated", generated!.Password, "Copy");
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

        bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this password?", "Yes", "No");
        if (!confirm) return;

        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "User is not authenticated.", "OK");
                return;
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5023/api/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"Passwords/{id}");
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Deleted", "Password deleted successfully!", "OK");
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
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Error", "User is not authenticated.", "OK");
                return;
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5023/api/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updatedEntry = new PasswordEntry
            {
                Id = passwordEntry.Id,
                Service = newService,
                Username = newUsername,
                EncryptedPassword = newPassword,
                UserId = passwordEntry.UserId
            };

            var response = await client.PutAsJsonAsync($"Passwords/{passwordEntry.Id}", updatedEntry);
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

    private class PasswordResponse
    {
        public string Password { get; set; } = string.Empty;
    }
}
