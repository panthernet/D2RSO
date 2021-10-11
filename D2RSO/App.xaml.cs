using System;
using System.IO;
using System.Windows;
using ControlzEx.Theming;
using D2RSO.Classes;

namespace D2RSO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DataClass Data { get; } = new();
        public static SettingsClass Settings { get; private set; } = new();

        private string _settingsFilePath;
        private string _logsFilePath;
        private string _folderPath;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Current.DispatcherUnhandledException += (_, args) => Logger.Log(args.Exception);

            // Set the application theme to Dark.Green
            ThemeManager.Current.ChangeTheme(this, "Dark.Red");

            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "D2RSO");
            _settingsFilePath = Path.Combine(_folderPath, "settings.json");

            _logsFilePath = Path.Combine(_folderPath, "log.txt");
            Logger.Initialize(_logsFilePath);

            try
            {
                if (!Directory.Exists(_folderPath))
                    Directory.CreateDirectory(_folderPath);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }

            Data.Load();
            Settings = SettingsClass.Load(_settingsFilePath);
            Settings.UpdatePath(_settingsFilePath);
        }
    }
}
