namespace EasyPass.App
{
    /// <summary>
    /// Centralized configuration for the EasyPass app.
    /// Change the API URL here when deploying to a different server.
    /// </summary>
    public static class AppConfig
    {
        // The base URL for the EasyPass API
        // Change this when deploying to a different server
        public static string ApiBaseUrl = "http://localhost:5023/";
    }
}
