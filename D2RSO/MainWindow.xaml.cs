﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using D2RSO.Classes;
using D2RSO.Classes.Data;
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

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Title = $"D2R Skill Overlay V1.0.0";

            IsMaxRestoreButtonEnabled = false;
            IsMinButtonEnabled = true;
            TrackerSlider.Value = App.Settings.FormScaleX * 10;

            PlayCommand = new ActionCommand(PlayCommandExecuted);
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

            CreateCounterWindow(false);
        }

        private void OnHookKeyPressed(object? sender, GlobalKeyboardHookEventArgs e)
        {
            if (e.KeyboardState == GlobalInputHook.KeyboardState.KeyDown)
            {
                var usedItems = new List<SkillDataItem>();
                // Now you can access both, the key and virtual code
                var loggedKey = sender == null ? (e.KeyboardData.Flags == 0 ? "MOUSE1" : "MOUSE2") : e.KeyboardData.Key.ToString();
                var result = App.Settings.SkillItems.Where(a => a.IsEnabled && a.SkillKey != null && a.SkillKey.Equals(loggedKey, StringComparison.OrdinalIgnoreCase)).ToList();
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

                result = App.Settings.SkillItems.Where(a => a.IsEnabled && a.SelectKey != null && a.SelectKey.Equals(loggedKey, StringComparison.OrdinalIgnoreCase)).ToList();
                if (result.Any())
                {
                    //select key pressed
                    foreach (var item in result)
                    {
                        usedItems.Add(item);
                        item.SelectKeyPressed();
                    }
                }

                var except = App.Settings.SkillItems.Except(usedItems).ToList();
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
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void Scaler_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.Settings.FormScaleX = e.NewValue / 10;
            App.Settings.FormScaleY = e.NewValue / 10;
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

        private void AddSkillButton_OnClick(object sender, RoutedEventArgs e)
        {
            //add new skill to the list
            var id = App.Settings.SkillItems.Any() ? App.Settings.SkillItems.Max(a => a.Id) + 1 : 1;
            var item = new SkillDataItem { IconFileName = App.Data.Skillicons.FirstOrDefault().Key, Id = id };
            App.Settings.SkillItems.Add(item);
            App.Settings.Save();
        }

        private void RemSkillButton_OnClick(object sender, RoutedEventArgs e)
        {
            //remove skill
            App.Settings.SkillItems.Remove((SkillDataItem)((Button)sender).CommandParameter);
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
    }
}
