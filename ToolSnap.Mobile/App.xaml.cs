using Microsoft.Extensions.DependencyInjection;

namespace ToolSnap.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            UserAppTheme = Preferences.Get("app_theme", "system") switch
            {
                "dark"  => AppTheme.Dark,
                "light" => AppTheme.Light,
                _       => AppTheme.Unspecified
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}