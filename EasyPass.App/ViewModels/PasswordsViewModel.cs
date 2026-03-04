// EasyPass.App/ViewModels/PasswordsViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPass.App.Models;
using EasyPass.App.Services;
using Microsoft.Maui.Storage;

namespace EasyPass.App.ViewModels
{
    // ViewModel for the Passwords page.
    // Owns the password list, loading state, and search filtering.
    // Add/Edit operations are initiated by code-behind (dialog prompts)
    // but the actual API calls happen here.
    public partial class PasswordsViewModel : ObservableObject
    {
        private readonly IPasswordService _passwordService;
        private readonly IAuthenticationService _authService;

        // The full unfiltered list — used as source for search filtering
        private List<PasswordEntryViewModel> _allPasswords = new();

        // The list shown in the UI — filters based on SearchText
        public ObservableCollection<PasswordEntryViewModel> Passwords { get; } = new();

        [ObservableProperty]
        private bool isLoading = false;

        // When SearchText changes, OnSearchTextChanged() is automatically called
        [ObservableProperty]
        private string searchText = "";

        // The code-behind subscribes to show toast messages (UI concern)
        public event Action<string>? ToastRequested;

        // The code-behind subscribes and navigates to LoginPage
        public event Action? LogoutRequested;

        // The code-behind subscribes to show a confirmation dialog before deleting
        public event Action<PasswordEntryViewModel>? DeleteConfirmationRequested;

        // The code-behind subscribes to show edit prompt dialogs
        public event Action<PasswordEntryViewModel>? EditDialogRequested;

        public PasswordsViewModel(IPasswordService passwordService, IAuthenticationService authService)
        {
            _passwordService = passwordService;
            _authService = authService;
        }

        // Loads all passwords from the API and populates the list
        [RelayCommand]
        public async Task LoadPasswordsAsync()
        {
            IsLoading = true;

            try
            {
                var passwordList = await _passwordService.GetAllPasswordsAsync();

                // Unsubscribe from old entries to avoid memory leaks
                foreach (var existing in _allPasswords)
                {
                    existing.DeleteRequested -= OnDeleteRequested;
                    existing.EditRequested -= OnEditRequested;
                    existing.CopyRequested -= OnCopyRequested;
                }

                _allPasswords.Clear();
                Passwords.Clear();

                // Wrap each PasswordEntry in a PasswordEntryViewModel
                foreach (var entry in passwordList)
                {
                    var entryVm = new PasswordEntryViewModel(entry);
                    entryVm.DeleteRequested += OnDeleteRequested;
                    entryVm.EditRequested += OnEditRequested;
                    entryVm.CopyRequested += OnCopyRequested;
                    _allPasswords.Add(entryVm);
                    Passwords.Add(entryVm);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Called by [ObservableProperty] generated code when SearchText changes
        partial void OnSearchTextChanged(string value)
        {
            Passwords.Clear();

            if (string.IsNullOrEmpty(value))
            {
                // Show all passwords
                foreach (var p in _allPasswords)
                    Passwords.Add(p);
                return;
            }

            // Filter by service or username
            foreach (var p in _allPasswords)
            {
                bool matchesService = !string.IsNullOrEmpty(p.Entry.Service) &&
                    p.Entry.Service.Contains(value, StringComparison.OrdinalIgnoreCase);
                bool matchesUsername = !string.IsNullOrEmpty(p.Entry.Username) &&
                    p.Entry.Username.Contains(value, StringComparison.OrdinalIgnoreCase);

                if (matchesService || matchesUsername)
                    Passwords.Add(p);
            }
        }

        // Creates a new password via the API.
        // Called by code-behind after collecting service/username/password from dialogs.
        public async Task<bool> AddPasswordAsync(string service, string username, string password)
        {
            IsLoading = true;
            try
            {
                var newEntry = new PasswordEntry
                {
                    Service = service,
                    Username = username,
                    EncryptedPassword = password
                };

                bool success = await _passwordService.CreatePasswordAsync(newEntry);
                if (success)
                    await LoadPasswordsAsync();

                return success;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Updates an existing password via the API.
        // Called by code-behind after collecting new values from dialogs.
        public async Task<bool> UpdatePasswordAsync(int id, string service, string username, string password)
        {
            IsLoading = true;
            try
            {
                // Find the original entry to preserve UserId
                var original = _allPasswords.FirstOrDefault(p => p.Entry.Id == id);
                if (original == null) return false;

                var updatedEntry = new PasswordEntry
                {
                    Id = original.Entry.Id,
                    Service = service,
                    Username = username,
                    EncryptedPassword = password,
                    UserId = original.Entry.UserId
                };

                bool success = await _passwordService.UpdatePasswordAsync(id, updatedEntry);
                if (success)
                    await LoadPasswordsAsync();

                return success;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Deletes a password via the API.
        // Called by code-behind after user confirms the deletion dialog.
        public async Task<bool> DeletePasswordAsync(int id)
        {
            IsLoading = true;
            try
            {
                bool success = await _passwordService.DeletePasswordAsync(id);
                if (success)
                {
                    await LoadPasswordsAsync();
                    ToastRequested?.Invoke("Password deleted successfully");
                }
                return success;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Generates a strong password via the API.
        // Returns the generated password string (or empty string on failure).
        [RelayCommand]
        public async Task<string> GeneratePasswordAsync()
        {
            IsLoading = true;
            try
            {
                return await _passwordService.GeneratePasswordAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Clears the token and tells code-behind to navigate to LoginPage
        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.ClearTokenAsync();
            LogoutRequested?.Invoke();
        }

        // Called when a PasswordEntryViewModel raises DeleteRequested.
        private void OnDeleteRequested(PasswordEntryViewModel entryVm)
        {
            DeleteConfirmationRequested?.Invoke(entryVm);
        }

        // Called when a PasswordEntryViewModel raises EditRequested.
        private void OnEditRequested(PasswordEntryViewModel entryVm)
        {
            EditDialogRequested?.Invoke(entryVm);
        }

        // Called when a PasswordEntryViewModel raises CopyRequested.
        // Handles clipboard copy and auto-clear (security feature).
        private async void OnCopyRequested(PasswordEntryViewModel entryVm)
        {
            await Clipboard.SetTextAsync(entryVm.Entry.EncryptedPassword);
            ToastRequested?.Invoke("Password copied (clears in 30s)");

            // Wait 30 seconds then clear the clipboard for security
            await Task.Delay(TimeSpan.FromSeconds(30));
            await Clipboard.SetTextAsync(string.Empty);
            ToastRequested?.Invoke("Clipboard cleared");
        }
    }
}
