using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using D2RSO.Classes;
using D2RSO.Classes.Data;

namespace D2RSO
{
    /// <summary>
    /// Interaction logic for CountersWindow.xaml
    /// </summary>
    public partial class CountersWindow : INotifyPropertyChanged
    {
        private bool _isPreview;
        public ObservableCollection<TrackerItem> SkillTrackerItems { get; } = new();

        public bool IsPreview
        {
            get => _isPreview;
            private set { _isPreview = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotPreview)); }
        }

        public bool IsNotPreview => !IsPreview;


        public CountersWindow()
        {
            InitializeComponent();
            DataContext = this;

            Border.MouseDown += (_, e) =>
            {
                if (e.ChangedButton != MouseButton.Left || IsNotPreview)
                    return;
                DragMove();
            };
            Top = App.Settings.TrackerY;
            Left = App.Settings.TrackerX;
            LocationChanged += (_, _) =>
            {
                App.Settings.TrackerX = (int)Left;
                App.Settings.TrackerY = (int)Top;
                App.Settings.Save();
            };

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!_isPreview)
            {
                var windowHwnd = new WindowInteropHelper(this).Handle;
                WindowsServices.SetWindowExTransparent(windowHwnd);
            }
        }



        public void RemTrackerItem(int id)
        {
            this.Dispatcher.Invoke(() =>
            {
                var item = SkillTrackerItems.FirstOrDefault(a => a.Data.Id == id);
                if (item != null)
                    SkillTrackerItems.Remove(item);
            });
        }

        public void AddTrackerItem(SkillDataItem item)
        {
            var old = SkillTrackerItems.FirstOrDefault(a => a.Data.Id == item.Id);
            if (old != null)
            {
                //update existing tracker
                old.CurrentTimeValue = old.Data.TimeLength;
                return;
            }
            //create new tracker
            var tr = new TrackerItem(item);
            tr.OnCompleted += RemTrackerItem;
            this.Dispatcher.Invoke(() =>
            {
                if (App.Settings.IsTrackerInsertToLeft)
                    SkillTrackerItems.Insert(0, tr);
                else SkillTrackerItems.Add(tr);
            });
        }

        public void SetPreview(bool isPreview)
        {
            IsPreview = isPreview;
            Border.IsHitTestVisible = isPreview;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class TrackerItem: INotifyPropertyChanged
    {
        private double _currentTimeValue;
        private readonly Timer _timer;
        private bool _isRedOverlayVisible;

        public bool IsRedOverlayVisible
        {
            get => _isRedOverlayVisible;
            set { _isRedOverlayVisible = value; OnPropertyChanged(); }
        }

        public SkillDataItem Data { get; set; }

        public event Action<int> OnCompleted;

        public double CurrentTimeValue
        {
            get => _currentTimeValue;
            set { _currentTimeValue = value;  OnPropertyChanged(); }
        }

        public TrackerItem(SkillDataItem data)
        {
            Data = data;
            CurrentTimeValue = Data.TimeLength;
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += (_, _) =>
            {
                if(App.Settings.IsRedTrackerOverlayEnabled)
                    IsRedOverlayVisible = (CurrentTimeValue-1) <= App.Settings.RedTrackerOverlaySec;

                if (_currentTimeValue == 0)
                {
                    Stop();
                }
                CurrentTimeValue--;
            };
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _currentTimeValue = 0;
            OnCompleted?.Invoke(Data.Id);
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
