using System;
using System.Windows;
using MetroRadiance.UI;
using Puchipro6Visualizer.Game;

namespace Puchipro6Visualizer {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable {
        public void Dispose() {
            GenericProcessManager.DestroyAll();
        }

        protected override void OnStartup(StartupEventArgs e) {
            DispatcherUnhandledException += (sender, args) => {
                var message = "ハンドルされていない例外：" + args.Exception.ToString();
                var title = "Pushipro6 Visualizer";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            };

            base.OnStartup(e);

            ThemeService.Current.Register(this, Theme.Dark, Accent.Blue);
        }

        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);
            Dispose();
        }
    }
}