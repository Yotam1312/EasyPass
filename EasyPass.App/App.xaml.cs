using EasyPass.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EasyPass.App
{
    public partial class App : Application
    {
        // Static property to access the DI service provider from anywhere in the app
        public static IServiceProvider Services { get; private set; } = null!;

        // Constructor receives the service provider from DI
        public App(IServiceProvider services)
        {
            InitializeComponent();

            // Store the service provider so we can use it to get pages
            Services = services;

            // Set the initial page of the app (wrapped in a navigation container)
            // Use GetRequiredService to get the LoginPage with all its dependencies
            MainPage = new NavigationPage(Services.GetRequiredService<LoginPage>());
        }

        /// <summary>
        /// Helper method to get a page with all its dependencies injected.
        /// Use this instead of "new PageName()" when navigating.
        /// </summary>
        public static T GetPage<T>() where T : class
        {
            return Services.GetRequiredService<T>();
        }
    }
}
