using EasySave.Models;
using System.Windows;

namespace EasySave.WPF
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var settings = new SettingsManager().LoadSettings();
            LogBootstrapper.Apply(settings);
            base.OnStartup(e);
        }
    }
}
