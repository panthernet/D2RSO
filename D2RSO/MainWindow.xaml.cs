using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using D2RSO.Classes;
using D2RSO.Classes.Data;
using D2RSO.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
using Button = System.Windows.Controls.Button;

namespace D2RSO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: INotifyPropertyChanged
    {
        #region Fields and props

        private bool _isPlaying;
        private GlobalInputHook _globalInputHook;
        private bool _isCounterPreviewVisible;

        public CountersWindow CounterWindow { get; private set; }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotPlaying)); }
        }

        public bool IsNotPlaying => !IsPlaying;

        public bool IsCounterPreviewVisible
        {
            get => _isCounterPreviewVisible;
            set { _isCounterPreviewVisible = value; OnPropertyChanged(); }
        }

        public ICommand PlayCommand { get; set; }
        public ICommand AddSkillCommand { get; set; }
        public ICommand AddProfileCommand { get; set; }
        public ICommand RemoveProfileCommand { get; set; }

        public TrackerProfile Profile
        {
            get => _profile;
            set { _profile = value;
                OnProfileSelectionChanged(); OnPropertyChanged(); }
        }

        public ObservableCollection<SkillDataItem> SelectedSkillItems { get; set; } = new();
        private TrackerProfile _profile;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Title = $"D2R Skill Overlay V1.0.4";

            IsMaxRestoreButtonEnabled = false;
            IsMinButtonEnabled = true;
//            TrackerSlider.Value = App.Settings.FormScaleX * 10;

            PlayCommand = new ActionCommand(PlayCommandExecuted);
            AddSkillCommand = new SimpleCommand(_ => Profile != null, AddSkillButton);
            AddProfileCommand = new SimpleCommand(_ => IsNotPlaying, AddProfileButton);
            RemoveProfileCommand = new SimpleCommand(_ => IsNotPlaying, RemoveProfileButtonClick);

            Profile = App.Settings.Profiles.FirstOrDefault(a => a.Id == App.Settings.LastSelectedProfileId) ?? App.Settings.Profiles.FirstOrDefault();

            Loaded += (_, _) =>
            {
                if(App.Settings.StartTrackerOnAppRun)
                    PlayCommandExecuted();
            };
        }

        private void RemoveProfileButtonClick(object obj)
        {
            this.Dispatcher.Invoke(async () =>
            {
                var result = await this.ShowMessageAsync("Confirmation",
                    "Delete selected profile with all related skills?",
                    MessageDialogStyle.AffirmativeAndNegative);
                if (result != MessageDialogResult.Affirmative)
                    return;

                SelectedSkillItems.Clear();
                var items = App.Settings.SkillItems.Where(a => a.ProfileId == Profile.Id).ToList();
                foreach (var item in items)
                    App.Settings.SkillItems.Remove(item);

                App.Settings.Profiles.Remove(Profile);
                Profile = App.Settings.Profiles.FirstOrDefault();
                App.Settings.Save();
            });
        }

        private void AddProfileButton(object obj)
        {
            this.Dispatcher.Invoke(async () =>
            {
                var result = await this.ShowInputAsync("Add new profile", "Enter name",
                    new MetroDialogSettings {AffirmativeButtonText = "Add"});
                if(result == null)
                    return;

                var id = App.Settings.Profiles.Any( )? App.Settings.Profiles.Max(a => a.Id) + 1 : 0;
                var item = new TrackerProfile {Id = id, Name = result};
                App.Settings.Profiles.Add(item);
                Profile = item;
                App.Settings.Save();

            });
        }

        #region Input tracking

        private void StartTracking()
        {
            IsPlaying = true;
            // Hooks into all keys.
            _globalInputHook = new GlobalInputHook();
            _globalInputHook.KeyboardPressed += OnHookKeyPressed;
            _globalInputHook.MouseButtonPressed += value => OnHookKeyPressed(null,
                new GlobalKeyboardHookEventArgs(new GlobalInputHook.LowLevelKeyboardInputEvent { Flags = value },
                    GlobalInputHook.KeyboardState.KeyDown));
            _globalInputHook.GamePadButtonPressed += button => OnHookKeyPressed(null,
                new GlobalKeyboardHookEventArgs(new GlobalInputHook.LowLevelKeyboardInputEvent() {Code = button},
                    GlobalInputHook.KeyboardState.KeyDown));

            CreateCounterWindow(false);
        }

        private void OnHookKeyPressed(object? sender, GlobalKeyboardHookEventArgs e)
        {
            if (e.KeyboardState == GlobalInputHook.KeyboardState.KeyDown)
            {
                var usedItems = new List<SkillDataItem>();
                // Now you can access both, the key and virtual code
                string loggedKey;
                if (sender == null)
                {
                    if(!string.IsNullOrEmpty(e.KeyboardData.Code))
                        loggedKey = e.KeyboardData.Code;
                    else
                    {
                        switch (e.KeyboardData.Flags)
                        {
                            case 0:
                                loggedKey = "MOUSE1";
                                break;
                            case 1:
                                loggedKey = "MOUSE2";
                                break;
                            case 2:
                                loggedKey = "MOUSE3";
                                break;
                            case 3:
                                loggedKey = "MOUSEX1";
                                break;
                            case 4:
                                loggedKey = "MOUSEX2";
                                break;
                            default: 
                                loggedKey = "UNKNOWN";
                                break;
                        }
                    }
                }
                else
                    loggedKey = e.KeyboardData.Key.ToString();

                var result = SelectedSkillItems.Where(a => a.IsEnabled && a.SkillKey != null && a.SkillKey.Code.Equals(loggedKey, StringComparison.OrdinalIgnoreCase)).ToList();
                if (result.Any())
                {
                    //signal skill key pressed
                    foreach (var item in result)
                    {
                        usedItems.Add(item);
                        if (item.SkillKeyPressed())
                        {
                            //do fire timer
                            CounterWindow.AddTrackerItem(item);
                        }
                    }
                }

                result = SelectedSkillItems.Where(a => a.IsEnabled && a.SelectKey != null && a.SelectKey.Code.Equals(loggedKey, StringComparison.OrdinalIgnoreCase)).ToList();
                if (result.Any())
                {
                    //select key pressed
                    foreach (var item in result)
                    {
                        usedItems.Add(item);
                        item.SelectKeyPressed();
                    }
                }

                var except = SelectedSkillItems.Except(usedItems).ToList();
                except.ForEach(a=> a.ResetKeys());
            }
        }

        private void StopTracking()
        {
            IsPlaying = false;
            _globalInputHook?.Dispose();
            _globalInputHook = null;
            CounterWindow?.Close();
            CounterWindow = null;
            IsCounterPreviewVisible = false;
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// Create tracker window
        /// </summary>
        /// <param name="isPreview">Is preview mode on</param>
        private void CreateCounterWindow(bool isPreview)
        {
            CounterWindow?.Close();
            CounterWindow = new CountersWindow();
            CounterWindow.SetPreview(isPreview);
            CounterWindow.Show();
        }

        private void AddSkillButton(object obj)
        {
            //add new skill to the list
            var id = App.Settings.SkillItems.Any() ? App.Settings.SkillItems.Max(a => a.Id) + 1 : 1;
            var item = new SkillDataItem { IconFileName = App.Data.Skillicons.FirstOrDefault().Key, Id = id, ProfileId = Profile.Id};
            App.Settings.SkillItems.Add(item);
            SelectedSkillItems.Add(item);
            App.Settings.Save();
        }

        private void RemSkillButton_OnClick(object sender, RoutedEventArgs e)
        {
            //remove skill
            var item = (SkillDataItem) ((Button) sender).CommandParameter;
            App.Settings.SkillItems.Remove(item);
            SelectedSkillItems.Remove(item);
            App.Settings.Save();
        }

        private void EyeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsCounterPreviewVisible)
            {
                IsCounterPreviewVisible = !IsCounterPreviewVisible;
                CounterWindow?.Close();
                CounterWindow = null;
            }
            else
            {
                IsCounterPreviewVisible = !IsCounterPreviewVisible;
                CreateCounterWindow(true);
            }
        }

        private void PlayCommandExecuted()
        {
            App.Settings.Save();

            if (IsPlaying)
                StopTracking();
            else
                StartTracking();
        }

        private async void SupportButton_OnClick(object sender, RoutedEventArgs e)
        {
            const string msg = "1. Add new skill, select icon\n2. Set skill duration (sec) \n3. Set skill select key (e.g. F8)\n4. Set skill activation key\n5. Press the EYE button to set up skill bar position\n6. Press run button to start\n\nOverlay will trigger once select and act buttons are pressed sequentially";
            await this.ShowMessageAsync("HOW TO FAQ", msg, MessageDialogStyle.Affirmative);
        }


        private void ExitButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ExitMenuItem_OnClick(sender, e);
        }


        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _globalInputHook?.Dispose();
            CounterWindow?.Close();
            App.Settings.Save();
            Application.Current.Shutdown(0);
        }

        private void OpenWindowMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsVisible)
                Hide();
            else Show();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var pi = new ProcessStartInfo("https://github.com/panthernet") { UseShellExecute = true };
            Process.Start(pi);
        }

        private async void OptionsButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OptionsDialog();
            var dialog = new CustomDialog(this)
            {
                Content = dlg
            };
            dlg.CloseButton.Click += async (_, _) => await this.HideMetroDialogAsync(dialog);

            await this.ShowMetroDialogAsync(dialog);
        }

        private void OnProfileSelectionChanged()
        {
            if (Profile != null)
            {
                App.Settings.LastSelectedProfileId = Profile.Id;
                App.Settings.Save();

                SelectedSkillItems.Clear();
                foreach (var item in App.Settings.SkillItems.Where(a => a.ProfileId == Profile.Id))
                {
                    SelectedSkillItems.Add(item);
                }

            }
            else SelectedSkillItems.Clear();
        }
    }
}
