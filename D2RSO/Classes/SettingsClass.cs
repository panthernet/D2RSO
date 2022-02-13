using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using D2RSO.Classes.Data;
using Newtonsoft.Json;

namespace D2RSO.Classes
{
    public class SettingsClass: INotifyPropertyChanged
    {
        private string _filePath;
        private double _formScaleX;
        private double _formScaleY;
        private bool _isTrackerInsertToLeft;
        private bool _isTrackerVertical;
        private bool _showDigitsInTracker;
        private int _redTrackerOverlaySec;
        private bool _startTrackerOnAppRun;

        public bool StartTrackerOnAppRun
        {
            get => _startTrackerOnAppRun;
            set { _startTrackerOnAppRun = value; OnPropertyChanged(); }
        }

        public bool ShowDigitsInTracker
        {
            get => _showDigitsInTracker;
            set { _showDigitsInTracker = value; OnPropertyChanged(); }
        }

        public bool IsTrackerVertical
        {
            get => _isTrackerVertical;
            set { _isTrackerVertical = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Last selected profile Id to load profile when the app starts
        /// </summary>
        public int LastSelectedProfileId { get; set; }

        /// <summary>
        /// Skills data collection
        /// </summary>
        public ObservableCollection<SkillDataItem> SkillItems { get; set; } = new();

        /// <summary>
        /// Skill profiles collection
        /// </summary>
        public ObservableCollection<TrackerProfile> Profiles { get; set; } = new();

        /// <summary>
        /// Tracker window X position coordinate
        /// </summary>
        public int TrackerX { get; set; }
        /// <summary>
        /// Tracker window Y position coordinate
        /// </summary>
        public int TrackerY { get; set; }

        /// <summary>
        /// Tracker window X Scale value
        /// </summary>
        public double FormScaleX
        {
            get => _formScaleX;
            set { _formScaleX = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Tracker window Y Scale value
        /// </summary>
        public double FormScaleY
        {
            get => _formScaleY;
            set { _formScaleY = value; OnPropertyChanged(); }
        }

        public bool IsTrackerInsertToLeft
        {
            get => _isTrackerInsertToLeft;
            set { _isTrackerInsertToLeft = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsRedTrackerOverlayEnabled => _redTrackerOverlaySec > 0;

        public int RedTrackerOverlaySec
        {
            get => _redTrackerOverlaySec;
            set { _redTrackerOverlaySec = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRedTrackerOverlayEnabled)); }
        }

        /// <summary>
        /// Save settings to a file
        /// </summary>
        public void Save()
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(this)); 
        }

        /// <summary>
        /// Load settings from a file
        /// </summary>
        /// <param name="filePath">File path</param>
        public static SettingsClass Load(string filePath)
        {
            SettingsClass result;
            try
            {
                result = File.Exists(filePath)
                    ? JsonConvert.DeserializeObject<SettingsClass>(File.ReadAllText(filePath))
                    : new SettingsClass();
            }
            catch
            {
                result = new SettingsClass();
            }

            if (!result.Profiles.Any()) result.Profiles.Add(new TrackerProfile {Id = 0, Name = "Default"});

            return result;
        }

        /// <summary>
        /// Update default path for settings file
        /// </summary>
        /// <param name="filepath">File path</param>
        public void UpdatePath(string filepath)
        {
            _filePath = filepath;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}