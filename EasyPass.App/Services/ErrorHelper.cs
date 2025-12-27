using System;
using System.Net.Http;

namespace EasyPass.App.Services
{
    /// <summary>
    /// Helper class to detect error types and provide user-friendly messages.
    /// This helps distinguish between network errors and other errors.
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// Checks if the exception is a network-related error.
        /// Returns true if the user has no internet connection.
        /// </summary>
        public static bool IsNetworkError(Exception ex)
        {
            // HttpRequestException is thrown when there's a network problem
            if (ex is HttpRequestException)
            {
                return true;
            }

            // Sometimes the network error is wrapped in another exception
            if (ex.InnerException is HttpRequestException)
            {
                return true;
            }

            // Check for common network error keywords in the message
            string message = ex.Message.ToLower();
            if (message.Contains("network") ||
                message.Contains("connection") ||
                message.Contains("host") ||
                message.Contains("unreachable"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a user-friendly error message based on the exception type.
        /// Network errors get a specific message, others get a generic message.
        /// </summary>
        public static string GetUserFriendlyMessage(Exception ex)
        {
            // Check if it's a network error
            if (IsNetworkError(ex))
            {
                return "No internet connection. Please check your network.";
            }

            // For all other errors, return a generic message
            return "Something went wrong. Please try again.";
        }
    }
}
