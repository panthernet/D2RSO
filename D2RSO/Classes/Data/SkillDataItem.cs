using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;


namespace D2RSO.Classes.Data
{
    /// <summary>
    /// Main skill data class
    /// </summary>
    public class SkillDataItem: INotifyPropertyChanged
    {
        private string _iconFileName;
        /// <summary>
        /// Stores press states: 0 - initial, 1 - select pressed
        /// </summary>
        private volatile int _state = 0;

        /// <summary>
        /// Internal Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Profile id for this skill item
        /// </summary>
        public int ProfileId { get; set; }

        /// <summary>
        /// File path for the image icon
        /// </summary>
        public string IconFileName
        {
            get => _iconFileName;
            set { _iconFileName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Stores duration of skill overlay
        /// </summary>
        public double TimeLength { get; set; } = 5.0d;
        /// <summary>
        /// Is skill enabled for tracking
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        /// <summary>
        /// Stores key to select skill in game
        /// </summary>
        public DataClass.KeyEntry SelectKey { get; set; }
        /// <summary>
        /// Stores key to activate skill in game
        /// </summary>
        public DataClass.KeyEntry SkillKey { get; set; } = App.Data.AvailableKeys.FirstOrDefault(a => a.Code == "MOUSE2");

        /// <summary>
        /// Process use skill key press
        /// </summary>
        public bool SkillKeyPressed()
        {
            //initial state with no select key
            if (_state == 0 && SelectKey == null)
                return true;
            //already pressed select
            if (_state == 1)
            {
                _state = 0;
                return true;
            }

            //not yet pressed select
            return false;
        }

        /// <summary>
        /// Process select skill key press
        /// </summary>
        public void SelectKeyPressed()
        {
            if (_state == 0)
                _state = 1;
        }

        /// <summary>
        /// Reset key press sequence
        /// </summary>
        public void ResetKeys()
        {
            _state = 0;
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