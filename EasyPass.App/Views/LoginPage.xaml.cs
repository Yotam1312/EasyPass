// EasyPass.App/Views/LoginPage.xaml.cs
using EasyPass.App.ViewModels;

namespace EasyPass.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    // Constructor receives LoginViewModel from Dependency Injection
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        // Set the ViewModel as the binding context so XAML bindings work
        BindingContext = viewModel;

        // Subscribe to the LoginSucceeded event for navigation
        viewModel.LoginSucceeded += OnLoginSucceeded;

        // Check biometric availability when the page loads
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        this.Loaded -= OnPageLoaded;
        await _viewModel.InitializeAsync();
    }

    // Called by ViewModel when login is successful — navigate to PasswordsPage
    private void OnLoginSucceeded()
    {
        // Clear the navigation stack so the user can't press back to return to login
        Application.Current!.MainPage = new NavigationPage(App.GetPage<PasswordsPage>());
    }

    // Register button still handled in code-behind (it's pure navigation)
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(App.GetPage<RegisterPage>());
    }
}
