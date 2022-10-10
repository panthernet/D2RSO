using System;
using System.IO;
using System.Linq;
using System.Threading;
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
        private static Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "DRSO_APP_INSTANCE";
            _mutex = new Mutex(true, appName, out var createdNew);
            if (!createdNew)
            {
                //app is already running! Exiting the application  
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            Current.DispatcherUnhandledException += (_, args) => Logger.Log(args.Exception);

            // Set the application theme to Dark.Green
            ThemeManager.Current.ChangeTheme(this, "Dark.Red");

            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "D2RSO");
            _settingsFilePath = Path.Combine(_folderPath, "settings.json");

            _logsFilePath = Path.Combine(_folderPath, "log.txt");
            Logger.Initialize(_logsFilePath);
            DispatcherUnhandledException += (sender, args) => Logger.Log(args.Exception);

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

            foreach (var item in Settings.SkillItems)
            {
                if (item.SelectKey != null)
                    item.SelectKey = Data.AvailableKeys.FirstOrDefault(a => a.Code == item.SelectKey.Code);
                if(item.SkillKey != null)
                    item.SkillKey = Data.AvailableKeys.FirstOrDefault(a => a.Code == item.SkillKey.Code);
            }

            Settings.UpdatePath(_settingsFilePath);
        }
    }
}
