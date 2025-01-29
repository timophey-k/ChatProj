using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatCommon;
using System.Reflection;
using System;
using System.IO;

namespace ChatLauncher
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow();
                window.Closed += Window_Closed;
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            var launcherForm = sender as MainWindow;
            if (!launcherForm.AllowLaunch)
                return;

            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileHelper.ChatClientFile);
            var clientDll = Assembly.LoadFile(file);
            var formType = clientDll.GetType("ChatClient.MainWindow");
            dynamic clientForm = Activator.CreateInstance(formType);
            clientForm.SetLoginInfo(launcherForm.Client, launcherForm.MyName);
            clientForm.Show();
        }
    }
}