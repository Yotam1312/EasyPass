using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using EasyPass.App.Views;

namespace EasyPass.App.Services
{
    public class AuthenticationHandler : DelegatingHandler
    {
        public AuthenticationHandler() : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add the JWT token to the request header if we have one
            var token = await SecureStorage.Default.GetAsync("jwt_token");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            // Send the request and get the response
            var response = await base.SendAsync(request, cancellationToken);

            // Check if we got 401 Unauthorized (token expired or invalid)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Clear the stored token since it's no longer valid
                SecureStorage.Default.Remove("jwt_token");

                // Navigate to LoginPage and show message on the main thread
                // (UI changes must happen on the main thread)
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Show message to user explaining why they were logged out
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Session Expired",
                            "Your session has expired. Please login again.",
                            "OK");
                    }

                    // Reset app to LoginPage (same pattern as logout)
                    Application.Current!.MainPage = new NavigationPage(new LoginPage());
                });
            }

            return response;
        }
    }
}