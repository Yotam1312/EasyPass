using EasyPass.App.Views;

namespace EasyPass.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set the initial page of the app (wrapped in a navigation container)
            MainPage = new NavigationPage(new LoginPage());
        }
    }
}
