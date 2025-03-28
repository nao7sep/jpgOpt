using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace jpgOpt
{
    public partial class App: Application
    {
        public override void Initialize ()
        {
            AvaloniaXamlLoader.Load (this);
        }

        public override void OnFrameworkInitializationCompleted ()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime xDesktop)
                xDesktop.MainWindow = new MainWindow ();

            base.OnFrameworkInitializationCompleted ();
        }
    }
}
