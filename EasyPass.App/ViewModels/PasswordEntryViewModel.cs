// EasyPass.App/ViewModels/PasswordEntryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyPass.App.Models;

namespace EasyPass.App.ViewModels
{
    // ViewModel for a single password entry in the list.
    // Handles the Show/Hide toggle and raises events for Copy, Edit, Delete
    // so the parent PasswordsViewModel can handle the actual operations.
    public partial class PasswordEntryViewModel : ObservableObject
    {
        // The password data from the API
        public PasswordEntry Entry { get; }

        // Tracks whether the password is visible or hidden
        // [NotifyPropertyChangedFor] tells the toolkit to also raise
        // PropertyChanged for DisplayPassword and ToggleButtonText
        // whenever IsPasswordVisible changes.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayPassword))]
        [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
        private bool isPasswordVisible = false;

        // Shows the actual password or "********" based on toggle state
        public string DisplayPassword => IsPasswordVisible
            ? (Entry.EncryptedPassword ?? "")
            : "********";

        // Button text changes based on toggle state
        public string ToggleButtonText => IsPasswordVisible ? "Hide" : "Show";

        // Events that the parent PasswordsViewModel subscribes to.
        // We use events instead of direct calls so this ViewModel
        // doesn't need to know about the service layer.
        public event Action<PasswordEntryViewModel>? DeleteRequested;
        public event Action<PasswordEntryViewModel>? EditRequested;
        public event Action<PasswordEntryViewModel>? CopyRequested;

        public PasswordEntryViewModel(PasswordEntry entry)
        {
            Entry = entry;
        }

        // Toggles password visibility - this is pure UI state, no API call
        [RelayCommand]
        private void TogglePassword()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        // Raises the CopyRequested event - parent handles the actual copy
        [RelayCommand]
        private void Copy()
        {
            CopyRequested?.Invoke(this);
        }

        // Raises the EditRequested event - parent handles the dialog + API call
        [RelayCommand]
        private void Edit()
        {
            EditRequested?.Invoke(this);
        }

        // Raises the DeleteRequested event - parent handles the confirmation + API call
        [RelayCommand]
        private void Delete()
        {
            DeleteRequested?.Invoke(this);
        }
    }
}
