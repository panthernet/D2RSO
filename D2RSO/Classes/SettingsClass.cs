using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        /// <summary>
        /// Skills data collection
        /// </summary>
        public ObservableCollection<SkillDataItem> SkillItems { get; set; } = new();

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
            return File.Exists(filePath) ? JsonConvert.DeserializeObject<SettingsClass>(File.ReadAllText(filePath)) : new SettingsClass();
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