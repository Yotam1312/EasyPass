using Microsoft.Extensions.Logging;
using EasyPass.App.Services;
using EasyPass.App.ViewModels;
using EasyPass.App.Views;

namespace EasyPass.App;

// Configures and builds the MAUI application
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ==============================
        // DEPENDENCY INJECTION SETUP
        // ==============================

        // Register AuthenticationHandler as transient (new instance each time)
        builder.Services.AddTransient<AuthenticationHandler>();

        // Register services via their interfaces.
        // This lets us swap implementations (e.g., mock services for testing).
        // Singleton = one instance shared by everyone (AuthService stores state)
        // Transient = new instance each time it's requested
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddTransient<IPasswordService, PasswordService>();

        // Register HttpClient for API calls (with /api/ base path)
        // This HttpClient is used by PasswordService for password operations
        builder.Services.AddHttpClient("EasyPassApi", client =>
        {
            client.BaseAddress = new Uri(AppConfig.ApiBaseUrl + "api/");
        })
        .AddHttpMessageHandler<AuthenticationHandler>();

        // Register HttpClient for Auth calls (without /api/ path)
        // This HttpClient is used by LoginPage and RegisterPage
        builder.Services.AddHttpClient("EasyPassAuth", client =>
        {
            client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
        })
        .AddHttpMessageHandler<AuthenticationHandler>();

        // Register ViewModels (transient = fresh state each time a page is opened)
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<PasswordsViewModel>();

        // Register pages so they can receive ViewModels through constructors
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<PasswordsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
