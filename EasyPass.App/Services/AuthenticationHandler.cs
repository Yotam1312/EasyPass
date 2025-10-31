using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

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
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}