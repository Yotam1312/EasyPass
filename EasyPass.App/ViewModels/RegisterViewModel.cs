// EasyPass.App/ViewModels/RegisterViewModel.cs
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EasyPass.App.ViewModels
{
    // ViewModel for the Register page.
    // Handles validation and API registration call.
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private string pin = "";

        [ObservableProperty]
        private string confirmPin = "";

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string errorMessage = "";

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // The code-behind subscribes and calls Navigation.PopAsync()
        public event Action? RegisterSucceeded;

        public RegisterViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = "";

            // Validate email
            if (string.IsNullOrEmpty(Email))
            {
                ErrorMessage = "Please enter your email.";
                return;
            }

            if (!Email.Contains("@"))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            // Validate PIN
            if (string.IsNullOrEmpty(Pin))
            {
                ErrorMessage = "Please enter a PIN.";
                return;
            }

            if (Pin.Length < 4)
            {
                ErrorMessage = "PIN must be at least 4 digits.";
                return;
            }

            bool isNumericPin = true;
            foreach (char c in Pin)
            {
                if (!char.IsDigit(c))
                {
                    isNumericPin = false;
                    break;
                }
            }
            if (!isNumericPin)
            {
                ErrorMessage = "PIN must contain only numbers.";
                return;
            }

            if (Pin != ConfirmPin)
            {
                ErrorMessage = "PINs do not match.";
                return;
            }

            IsLoading = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient("EasyPassAuth");
                var registerRequest = new { Username = Email, Pin = Pin };

                var response = await httpClient.PostAsJsonAsync("api/auth/register", registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    // Tell code-behind to navigate back to login
                    RegisterSucceeded?.Invoke();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Registration failed: {errorBody}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException != null
                    ? $"{ex.Message} | {ex.InnerException.Message}"
                    : ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
