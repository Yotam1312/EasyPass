using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using EasyPass.App.Views;

namespace EasyPass.App.Services
{
    public class AuthenticationHandler : DelegatingHandler
    {
        // Empty constructor for DI - the HttpClientFactory will set InnerHandler
        public AuthenticationHandler()
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Try to add the JWT token to the request header if we have one
            // SecureStorage can fail on Windows in debug mode, so we wrap it in try-catch
            try
            {
                var token = await SecureStorage.Default.GetAsync("jwt_token");

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // SecureStorage failed (common on Windows debug mode)
                // Continue without the token - registration and login don't need it
            }

            // Send the request and get the response
            var response = await base.SendAsync(request, cancellationToken);

            // Check if we got 401 Unauthorized (token expired or invalid)
            // But skip this for login/register endpoints - they handle their own 401 errors
            string requestPath = request.RequestUri?.AbsolutePath ?? "";
            bool isAuthEndpoint = requestPath.Contains("/auth/login") || requestPath.Contains("/auth/register");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !isAuthEndpoint)
            {
                // Clear the stored token since it's no longer valid
                try
                {
                    SecureStorage.Default.Remove("jwt_token");
                }
                catch
                {
                    // SecureStorage might fail on Windows, ignore
                }

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
                    // Use App.GetPage to get the page through DI
                    Application.Current!.MainPage = new NavigationPage(App.GetPage<LoginPage>());
                });
            }

            return response;
        }
    }
}