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
        public List<KeyEntry> AvailableKeys { get; } = new();

        public class KeyEntry
        {
            public string Name { get; set; }
            public string Code { get; set; }

            public KeyEntry() {}

            public KeyEntry(string key)
            {
                Name = Code = key;
            }

            public KeyEntry(string name, string key)
            {
                Name = name;
                Code = key;
            }
        }

        private const string KEYS = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,Comma|OemComma,~|OemTilde,[|OemOpenBrackets,]|OemCloseBrackets,:|OemSemicolon,'|OemQuotes,1|D1,2|D2,3|D3,4|D4,5|D5,6|D6,7|D7,8|D8,9|D9,0|D0,+|Add,-|Subtract,Esc|Escape,Enter,Return,Left Shift|LShiftKey,Right Shift|RShiftKey,Left Alt|LMenu,Right Alt|RMenu,Left Control|LControlKey,Right Control|RControlKey,F1,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11,F12,NumPad0,NumPad1,NumPad2,NumPad3,NumPad4,NumPad5,NumPad6,NumPad7,NumPad8,NumPad9,Tab,Back,MOUSE1,MOUSE2,MOUSE3,MOUSEX1,MOUSEX2,GamePad Button 0|Buttons0,GamePad Button 1|Buttons1,GamePad Button 2|Buttons2,GamePad Button 3|Buttons3,GamePad Button 4|Buttons4,GamePad Button 5|Buttons5,GamePad Button 6|Buttons6,GamePad Button 7|Buttons7,GamePad Button 8|Buttons8,GamePad Button 9|Buttons9";

        /// <summary>
        /// Load all images for faster access
        /// </summary>
        public void Load()
        {
            AvailableKeys.Clear();
            foreach (var key in KEYS.Split(','))
            {
                var split = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
                AvailableKeys.Add(split.Length == 1 ? new KeyEntry(split[0]) : new KeyEntry(split[0], split[1]));
            }

            AvailableKeys.Add(new KeyEntry(null));

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