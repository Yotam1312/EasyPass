// EasyPass.App/Views/PasswordsPage.xaml.cs
using EasyPass.App.Services;
using EasyPass.App.ViewModels;

namespace EasyPass.App.Views;

public partial class PasswordsPage : ContentPage
{
    private readonly PasswordsViewModel _viewModel;
    private readonly IAuthenticationService _authService;
    private IDispatcherTimer? _toastTimer;

    // Constructor receives PasswordsViewModel and IAuthenticationService from DI
    public PasswordsPage(PasswordsViewModel viewModel, IAuthenticationService authService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _authService = authService;
        BindingContext = viewModel;

        // Subscribe to ViewModel events that require UI interaction
        viewModel.ToastRequested += ShowToast;
        viewModel.LogoutRequested += OnLogoutRequested;
        viewModel.DeleteConfirmationRequested += OnDeleteConfirmationRequested;
        viewModel.EditDialogRequested += OnEditDialogRequested;

        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        this.Loaded -= OnPageLoaded;
        await CheckAuthAndLoadPasswordsAsync();
    }

    // Checks biometric auth then loads passwords
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

            await _viewModel.LoadPasswordsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Authentication failed: {ex.Message}", "OK");
            await Navigation.PopAsync();
        }
    }

    // Shows a dialog to add a new password — dialog is UI, API call is ViewModel
    private async void OnAddPasswordClicked(object sender, EventArgs e)
    {
        string service = "";
        string username = "";
        string password = "";
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
                    password = await _viewModel.GeneratePasswordAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ErrorHelper.GetUserFriendlyMessage(ex), "OK");
                    continue;
                }
            }
            else
            {
                var passwordInput = await DisplayPromptAsync("Add Password", "Password:", initialValue: password);
                if (passwordInput == null) return;
                password = passwordInput;
            }

            bool confirm = await DisplayAlert("Save Password",
                $"Service: {service}\nUsername: {username}\nPassword: {password}",
                "Save", "Edit");
            if (!confirm) continue;

            bool success = await _viewModel.AddPasswordAsync(service, username, password);
            if (success)
            {
                await DisplayAlert("Success", "Password saved!", "OK");
                isSaved = true;
            }
            else
            {
                await DisplayAlert("Error", "Failed to save password.", "OK");
            }
        }
    }

    // Generates a password and shows it in an alert
    private async void OnGeneratePasswordClicked(object sender, EventArgs e)
    {
        try
        {
            string password = await _viewModel.GeneratePasswordAsync();
            await DisplayAlert("Strong Password Generated", password, "Copy");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ErrorHelper.GetUserFriendlyMessage(ex), "OK");
        }
    }

    // Called by ViewModel's DeleteConfirmationRequested event
    private async void OnDeleteConfirmationRequested(PasswordEntryViewModel entryVm)
    {
        bool confirm = await DisplayAlert(
            "Delete Password",
            $"Are you sure you want to delete the password for {entryVm.Entry.Service}?\n\nThis action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        bool success = await _viewModel.DeletePasswordAsync(entryVm.Entry.Id);
        if (!success)
            await DisplayAlert("Error", "Failed to delete password.", "OK");
    }

    // Called by ViewModel's EditDialogRequested event
    private async void OnEditDialogRequested(PasswordEntryViewModel entryVm)
    {
        string newService = await DisplayPromptAsync("Edit Password", "Service:",
            initialValue: entryVm.Entry.Service) ?? entryVm.Entry.Service;

        string newUsername = await DisplayPromptAsync("Edit Password", "Username:",
            initialValue: entryVm.Entry.Username) ?? entryVm.Entry.Username;

        string newPassword = await DisplayPromptAsync("Edit Password", "Password:",
            initialValue: entryVm.Entry.EncryptedPassword) ?? entryVm.Entry.EncryptedPassword;

        bool success = await _viewModel.UpdatePasswordAsync(
            entryVm.Entry.Id, newService, newUsername, newPassword);

        if (success)
            await DisplayAlert("Success", "Password updated!", "OK");
        else
            await DisplayAlert("Error", "Failed to update password.", "OK");
    }

    // Called by ViewModel's LogoutRequested event — navigate to LoginPage
    private void OnLogoutRequested()
    {
        Application.Current!.MainPage = new NavigationPage(App.GetPage<LoginPage>());
    }

    // Shows a brief toast notification at the bottom of the screen
    private void ShowToast(string message)
    {
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

        var mainGrid = (Grid)Content;
        mainGrid.Add(toast);
        toast.SetValue(Grid.RowSpanProperty, 2);
        toast.VerticalOptions = LayoutOptions.End;
        toast.Margin = new Thickness(32, 0, 32, 32);
        toast.FadeTo(1, 200);

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
