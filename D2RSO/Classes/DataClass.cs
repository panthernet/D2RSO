using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace D2RSO.Classes
{
    public class DataClass
    {
        public Dictionary<string, ImageSource> Skillicons { get; } = new();

        /// <summary>
        /// The list of avalable keys
        /// </summary>
        public List<string> AvailableKeys { get; } = new()
        {
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8","MOUSE1","MOUSE2", null
        };

        private const string KEYS = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,OemComma,OemTilde,OemOpenBrackets,OemCloseBrackets,OemSemicolon,OemQuotes,1,2,3,4,5,6,7,8,9,0,Add,Subtract,Escape,Enter,Return,Shift,Alt,Control,F1,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11,F12,NumPad0,NumPad1,NumPad2,NumPad3,NumPad4,NumPad5,NumPad6,NumPad7,NumPad8,NumPad9,Tab,Back,MOUSE1,MOUSE2";

        /// <summary>
        /// Load all images for faster access
        /// </summary>
        public void Load()
        {
            AvailableKeys.Clear();
            foreach (var key in KEYS.Split(','))
                AvailableKeys.Add(key);
            AvailableKeys.Add(null);

            foreach (var fileName in Directory.EnumerateFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images/Skills")))
            {
                try
                {
                    var img = new BitmapImage(new Uri(fileName)) { CacheOption = BitmapCacheOption.OnLoad };
                    Skillicons.Add(fileName.ToLower(), img);
                }
                catch
                {
                    // skip
                }
            }
        }
    }
}