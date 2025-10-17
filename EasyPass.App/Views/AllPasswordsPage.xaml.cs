using System.Collections.Generic;
using Microsoft.Maui.Controls;
using EasyPass.App.Models;

namespace EasyPass.App.Views;

public partial class AllPasswordsPage : ContentPage
{
    private List<PasswordEntry> _passwords;

    public AllPasswordsPage(List<PasswordEntry> passwords)
    {
        InitializeComponent();
        _passwords = passwords;
        AllPasswordsList.ItemsSource = _passwords;
    }

    // Handles edit button click and updates the selected password entry
    private async void OnEditPasswordClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        if (button.CommandParameter is not int id)
            return;

        var passwordEntry = _passwords.Find(p => p.Id == id);
        if (passwordEntry == null)
        {
            await DisplayAlert("Error", "Password entry not found.", "OK");
            return;
        }

        string newService = await DisplayPromptAsync("Edit Password", "Service:", initialValue: passwordEntry.Service) ?? passwordEntry.Service;
        string newUsername = await DisplayPromptAsync("Edit Password", "Username:", initialValue: passwordEntry.Username) ?? passwordEntry.Username;
        string newPassword = await DisplayPromptAsync("Edit Password", "Password:", initialValue: passwordEntry.EncryptedPassword) ?? passwordEntry.EncryptedPassword;

        // Update in-memory list (replace with API update in production)
        passwordEntry.Service = newService;
        passwordEntry.Username = newUsername;
        passwordEntry.EncryptedPassword = newPassword;

        AllPasswordsList.ItemsSource = null;
        AllPasswordsList.ItemsSource = _passwords;
    }

    // Handles delete button click and removes the selected password entry
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        if (button.CommandParameter is not int id)
            return;

        var passwordEntry = _passwords.Find(p => p.Id == id);
        if (passwordEntry == null)
        {
            await DisplayAlert("Error", "Password entry not found.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this password?", "Yes", "No");
        if (!confirm) return;

        // Remove from in-memory list (replace with API call in production)
        _passwords.Remove(passwordEntry);
        AllPasswordsList.ItemsSource = null;
        AllPasswordsList.ItemsSource = _passwords;
    }
}
