using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using EasyPass.App.Models;
using EasyPass.App.Services;

namespace EasyPass.App.Views;

public partial class PasswordsPage : ContentPage
{
    // Services are injected through the constructor (Dependency Injection)
    private readonly PasswordService _passwordService;
    private readonly AuthenticationService _authService;

    private List<PasswordEntry> _allPasswords = new();
    private IDispatcherTimer? _toastTimer;
    private IDispatcherTimer? _clipboardClearTimer;  // Timer to auto-clear clipboard for security
    private bool _isLoading = false;  // Track if we're currently doing an async operation

    // Constructor receives services from Dependency Injection
    public PasswordsPage(PasswordService passwordService, AuthenticationService authService)
    {
        InitializeComponent();

        // Store the injected services
        _passwordService = passwordService;
        _authService = authService;

        // Use Loaded event for safer async initialization
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Unsubscribe to prevent multiple calls
        this.Loaded -= OnPageLoaded;
        await CheckAuthAndLoadPasswordsAsync();
    }

    /// <summary>
    /// Shows or hides the loading indicator and disables/enables buttons.
    /// This prevents double-tapping and shows the user something is happening.
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        _isLoading = isLoading;

        // Show/hide the loading spinner
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;

        // Hide/show the password list (so loading indicator is visible)
        PasswordsList.IsVisible = !isLoading;

        // Disable/enable buttons to prevent double-tap
        AddButton.IsEnabled = !isLoading;
        GenerateButton.IsEnabled = !isLoading;
        LogoutButton.IsEnabled = !isLoading;
    }

    private async Task CheckAuthAndLoadPasswordsAsync()
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

            // Show loading indicator while fetching passwords
            SetLoadingState(true);

            // Use the PasswordService to get all passwords
            _allPasswords = await _passwordService.GetAllPasswordsAsync();
            PasswordsList.ItemsSource = _allPasswords;
        }
        catch (Exception ex)
        {
            // Get user-friendly error message
            string message = ErrorHelper.GetUserFriendlyMessage(ex);

            // Ask user if they want to retry
            bool retry = await DisplayAlert(
                "Error",
                message,
                "Retry",
                "Cancel");

            if (retry)
            {
                // Try loading passwords again
                await LoadPasswords();
            }
        }
        finally
        {
            // Always hide loading indicator when done
            SetLoadingState(false);
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
                    // Use the PasswordService to generate a strong password
                    password = await _passwordService.GeneratePasswordAsync();
                }
                catch (Exception ex)
                {
                    // Show user-friendly error message
                    string message = ErrorHelper.GetUserFriendlyMessage(ex);
                    await DisplayAlert("Error", message, "OK");
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
                // Show loading while saving to API
                SetLoadingState(true);

                var newPassword = new PasswordEntry
                {
                    Service = service,
                    Username = username,
                    EncryptedPassword = password
                };

                // Use the PasswordService to create the password
                bool success = await _passwordService.CreatePasswordAsync(newPassword);
                if (success)
                {
                    await DisplayAlert("Success", "Password saved!", "OK");
                    await LoadPasswords();
                    isSaved = true;
                }
                else
                {
                    await DisplayAlert("Error", "Failed to save password.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Show user-friendly error message
                string message = ErrorHelper.GetUserFriendlyMessage(ex);
                await DisplayAlert("Error", message, "OK");
            }
            finally
            {
                SetLoadingState(false);
            }
        }
    }

    private async void OnAddPasswordClicked(object sender, EventArgs e)
    {
        // Prevent double-tap if already loading
        if (_isLoading) return;

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
        // Prevent double-tap if already loading
        if (_isLoading) return;

        try
        {
            // Show loading while generating password
            SetLoadingState(true);

            // Use the PasswordService to generate a strong password
            string password = await _passwordService.GeneratePasswordAsync();
            await DisplayAlert("Strong Password Generated", password, "Copy");
        }
        catch (Exception ex)
        {
            // Show user-friendly error message
            string message = ErrorHelper.GetUserFriendlyMessage(ex);
            await DisplayAlert("Error", message, "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // Deletes a password entry after confirmation
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        // Prevent double-tap if already loading
        if (_isLoading) return;

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
            // Show loading while deleting
            SetLoadingState(true);

            // Use the PasswordService to delete the password
            bool success = await _passwordService.DeletePasswordAsync(id);
            if (success)
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
            // Show user-friendly error message
            string message = ErrorHelper.GetUserFriendlyMessage(ex);
            await DisplayAlert("Error", message, "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // Edits an existing password entry and updates it through the API
    private async void OnEditPasswordClicked(object sender, EventArgs e)
    {
        // Prevent double-tap if already loading
        if (_isLoading) return;

        var button = (Button)sender;
        if (button.CommandParameter is not int id)
            return;

        var passwordEntry = _allPasswords.FirstOrDefault(p => p.Id == id);
        if (passwordEntry == null)
        {
            await DisplayAlert("Error", "Password entry not found.", "OK");
            return;
        }

        // Get user input (these are dialog prompts, not API calls, so no loading needed here)
        string newService = await DisplayPromptAsync("Edit Password", "Service:", initialValue: passwordEntry.Service) ?? passwordEntry.Service;
        string newUsername = await DisplayPromptAsync("Edit Password", "Username:", initialValue: passwordEntry.Username) ?? passwordEntry.Username;
        string newPassword = await DisplayPromptAsync("Edit Password", "Password:", initialValue: passwordEntry.EncryptedPassword) ?? passwordEntry.EncryptedPassword;

        try
        {
            // Show loading while saving to API
            SetLoadingState(true);

            var updatedEntry = new PasswordEntry
            {
                Id = passwordEntry.Id,
                Service = newService,
                Username = newUsername,
                EncryptedPassword = newPassword,
                UserId = passwordEntry.UserId
            };

            // Use the PasswordService to update the password
            bool success = await _passwordService.UpdatePasswordAsync(passwordEntry.Id, updatedEntry);
            if (success)
            {
                await DisplayAlert("Success", "Password updated!", "OK");
                await LoadPasswords();
            }
            else
            {
                await DisplayAlert("Error", "Failed to update password.", "OK");
            }
        }
        catch (Exception ex)
        {
            // Show user-friendly error message
            string message = ErrorHelper.GetUserFriendlyMessage(ex);
            await DisplayAlert("Error", message, "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // Shows/hides the password when the Show button is clicked
    private async void OnShowPasswordClicked(object sender, EventArgs e)
    {
        try
        {
            // Get the button and the password entry
            var button = (Button)sender;
            var passwordEntry = (PasswordEntry)button.CommandParameter;

            // Find the parent Frame > Grid > StackLayout that contains the password label
            var grid = (Grid)button.Parent.Parent;

            // Find the password label (in the 3rd row, which is the StackLayout at index 2)
            var passwordStack = (StackLayout)grid.Children[2];
            var passwordLabel = (Label)passwordStack.Children[1];

            // Toggle between showing and hiding the password
            if (button.Text == "Show")
            {
                // Show the actual password
                passwordLabel.Text = passwordEntry.EncryptedPassword;
                button.Text = "Hide";
            }
            else
            {
                // Hide the password
                passwordLabel.Text = "********";
                button.Text = "Show";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to toggle password visibility", "OK");
        }
    }

    private async void OnCopyPasswordClicked(object sender, EventArgs e)
    {
        try
        {
            var button = (Button)sender;
            var passwordEntry = (PasswordEntry)button.CommandParameter;

            await Clipboard.SetTextAsync(passwordEntry.EncryptedPassword);
            ShowToast("Password copied (clears in 30s)");

            // Start clipboard auto-clear timer for security
            StartClipboardClearTimer();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to copy password", "OK");
        }
    }

    /// <summary>
    /// Starts a timer to clear the clipboard after 30 seconds.
    /// This is a security feature to prevent password theft from clipboard.
    /// </summary>
    private void StartClipboardClearTimer()
    {
        // Stop any existing timer first
        _clipboardClearTimer?.Stop();

        // Create a new timer
        _clipboardClearTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_clipboardClearTimer != null)
        {
            _clipboardClearTimer.Interval = TimeSpan.FromSeconds(30);
            _clipboardClearTimer.Tick += async (s, e) =>
            {
                // Clear the clipboard
                await Clipboard.SetTextAsync(string.Empty);

                // Show a brief notification
                ShowToast("Clipboard cleared");

                // Stop the timer (one-time execution)
                _clipboardClearTimer.Stop();
            };
            _clipboardClearTimer.Start();
        }
    }

    // Handles the Logout button click
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Ask for confirmation before logging out
        bool confirm = await DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No");

        if (!confirm)
        {
            return;
        }

        // Clear the stored token
        await _authService.ClearTokenAsync();

        // Stop any running timers
        _clipboardClearTimer?.Stop();
        _toastTimer?.Stop();

        // Navigate back to login page and clear navigation stack
        // Use App.GetPage to get the page through DI
        Application.Current!.MainPage = new NavigationPage(App.GetPage<LoginPage>());
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
}
