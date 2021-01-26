using Elegance.Windows;
using MahApps.Metro;
using System;
using System.Windows;

namespace Elegance
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            new MainWindow(e.Args).Show();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.AddAccent("BaseLight", new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml"));
            ThemeManager.AddAccent("BaseDark", new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseDark.xaml"));

            base.OnStartup(e);
        }
    }
}
