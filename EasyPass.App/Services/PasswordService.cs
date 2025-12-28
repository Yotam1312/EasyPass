using System.Net.Http.Json;
using EasyPass.App.Models;

namespace EasyPass.App.Services
{
    /// <summary>
    /// Service class that handles all password-related API operations.
    /// This centralizes the API calls so they're not scattered in the UI code.
    /// </summary>
    public class PasswordService
    {
        // HttpClient is injected through the constructor
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Constructor - HttpClient is provided by Dependency Injection.
        /// </summary>
        public PasswordService(IHttpClientFactory httpClientFactory)
        {
            // Get the named HttpClient we registered in MauiProgram.cs
            _httpClient = httpClientFactory.CreateClient("EasyPassApi");
        }

        /// <summary>
        /// Gets all passwords for the current user.
        /// Returns an empty list if there's an error.
        /// </summary>
        public async Task<List<PasswordEntry>> GetAllPasswordsAsync()
        {
            // Call the API and get the response
            var passwords = await _httpClient.GetFromJsonAsync<List<PasswordEntry>>("Passwords");

            // Return the passwords, or an empty list if null
            if (passwords == null)
            {
                return new List<PasswordEntry>();
            }
            return passwords;
        }

        /// <summary>
        /// Creates a new password entry.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public async Task<bool> CreatePasswordAsync(PasswordEntry newPassword)
        {
            // Send POST request to create the password
            var response = await _httpClient.PostAsJsonAsync("Passwords", newPassword);

            // Return whether it was successful
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Updates an existing password entry.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(int id, PasswordEntry updatedPassword)
        {
            // Send PUT request to update the password
            var response = await _httpClient.PutAsJsonAsync($"Passwords/{id}", updatedPassword);

            // Return whether it was successful
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Deletes a password entry by its ID.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public async Task<bool> DeletePasswordAsync(int id)
        {
            // Send DELETE request to remove the password
            var response = await _httpClient.DeleteAsync($"Passwords/{id}");

            // Return whether it was successful
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Generates a strong random password using the API.
        /// Returns the generated password, or empty string if there's an error.
        /// </summary>
        public async Task<string> GeneratePasswordAsync()
        {
            // Call the API to generate a password
            var response = await _httpClient.GetFromJsonAsync<GeneratedPasswordResponse>("Utils/generate-password");

            // Return the password, or empty string if null
            if (response == null || string.IsNullOrEmpty(response.Password))
            {
                return string.Empty;
            }
            return response.Password;
        }

        /// <summary>
        /// Helper class to deserialize the password generation response.
        /// </summary>
        private class GeneratedPasswordResponse
        {
            public string Password { get; set; } = string.Empty;
        }
    }
}
