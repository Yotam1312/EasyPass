// EasyPass.App/Views/RegisterPage.xaml.cs
using EasyPass.App.ViewModels;

namespace EasyPass.App.Views;

public partial class RegisterPage : ContentPage
{
    // Constructor receives RegisterViewModel from Dependency Injection
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Navigate back to login when registration succeeds
        viewModel.RegisterSucceeded += OnRegisterSucceeded;
    }

    private async void OnRegisterSucceeded()
    {
        await DisplayAlert("Success", "Account created successfully! You can now log in.", "OK");
        await Navigation.PopAsync();
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
