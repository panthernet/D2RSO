﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace D2RSO.Classes
{
    internal class GlobalKeyboardHookEventArgs : HandledEventArgs
    {
        public GlobalInputHook.KeyboardState KeyboardState { get; private set; }
        public GlobalInputHook.LowLevelKeyboardInputEvent KeyboardData { get; private set; }

        public GlobalKeyboardHookEventArgs(
            GlobalInputHook.LowLevelKeyboardInputEvent keyboardData,
            GlobalInputHook.KeyboardState keyboardState)
        {
            KeyboardData = keyboardData;
            KeyboardState = keyboardState;
        }
    }

    //Based on https://gist.github.com/Stasonix
    internal class GlobalInputHook : IDisposable
    {
        public event EventHandler<GlobalKeyboardHookEventArgs> KeyboardPressed;
        public event Action<int> MouseButtonPressed;
        public event Action<int> GamePadButtonPressed;

        // EDT: Added an optional parameter (registeredKeys) that accepts keys to restict
        // the logging mechanism.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registeredKeys">Keys that should trigger logging. Pass null for full logging.</param>
        public GlobalInputHook(Keys[] registeredKeys = null)
        {
            _gamePad = new Gamepad();
            _gamePad.evNewGamePadButtonInfoAcquired += (sender, button, pressed) =>
            {
                if (pressed)
                    GamePadButtonPressed?.Invoke(button);
            };

            RegisteredKeys = registeredKeys;
            _windowsHookHandle = IntPtr.Zero;
            _user32LibraryHandle = IntPtr.Zero;
            _hookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.
            _mouseHookProc = LowLevelMouseProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

            _user32LibraryHandle = LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }



            _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle, 0);
            if (_windowsHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }

            _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, _user32LibraryHandle, 0);
            if (_mouseHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust mouse hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _gamePad?.Dispose();
            _gamePad = null;

            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                if (_windowsHookHandle != IntPtr.Zero)
                {
                    if (!UnhookWindowsHookEx(_windowsHookHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _windowsHookHandle = IntPtr.Zero;

                    if (!UnhookWindowsHookEx(_mouseHookHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove mouse hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _mouseHookHandle = IntPtr.Zero;

                    // ReSharper disable once DelegateSubtraction
                    _hookProc -= LowLevelKeyboardProc;
                    _mouseHookProc -= LowLevelMouseProc;
                }
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }
                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        ~GlobalInputHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IntPtr _windowsHookHandle;
        private IntPtr _mouseHookHandle;
        private IntPtr _user32LibraryHandle;
        private HookProc _hookProc;
        private HookProc _mouseHookProc;

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
        /// You would install a hook procedure to monitor the system for certain types of events. These events are
        /// associated either with a specific thread or with all threads in the same desktop as the calling thread.
        /// </summary>
        /// <param name="idHook">hook type</param>
        /// <param name="lpfn">hook procedure</param>
        /// <param name="hMod">handle to application instance</param>
        /// <param name="dwThreadId">thread identifier</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>
        /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">handle to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hHook);

        /// <summary>
        /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
        /// A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hHook">handle to current hook</param>
        /// <param name="code">hook code passed to hook procedure</param>
        /// <param name="wParam">value passed to hook procedure</param>
        /// <param name="lParam">value passed to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public Point pt;
            public int mouseData;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,

            WM_BTN3 = 0x000000000000020b,
            WM_BTN_MID = 0x0000000000000207,

        }

        [Flags()]
        public enum RawMouseButtons : ushort
        {
            None = 0,
            LeftDown = 0x0001,
            LeftUp = 0x0002,
            RightDown = 0x0004,
            RightUp = 0x0008,
            MiddleDown = 0x0010,
            MiddleUp = 0x0020,
            Button4Down = 0x0040,
            Button4Up = 0x0080,
            Button5Down = 0x0100,
            Button5Up = 0x0200,
            MouseWheel = 0x0400
        }

        [Flags()]
        public enum RawMouseFlags : ushort
        {
            MOVE_RELATIVE = 0,
            MOVE_ABSOLUTE = 1,
            VIRTUAL_DESKTOP = 2,
            ATTRIBUTES_CHANGED = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputMouse
        {
            public RawMouseFlags flags;
            public ushort buttonData;
            public RawMouseButtons buttonflags;
            public uint rawButtons;
            public int lastX;
            public int lastY;
            public uint extraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelKeyboardInputEvent
        {
            /// <summary>
            /// A virtual-key code. The code must be a value in the range 1 to 254.
            /// </summary>
            public int VirtualCode;

            // EDT: added a conversion from VirtualCode to Keys.
            /// <summary>
            /// The VirtualCode converted to typeof(Keys) for higher usability.
            /// </summary>
            public Keys Key { get { return (Keys)VirtualCode; } }

            /// <summary>
            /// A hardware scan code for the key. 
            /// </summary>
            public int HardwareScanCode;

            /// <summary>
            /// The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
            /// </summary>
            public int TimeStamp;

            /// <summary>
            /// Additional information associated with the message. 
            /// </summary>
            public IntPtr AdditionalInformation;
        }

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        //const int HC_ACTION = 0;

        public enum KeyboardState
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105
        }

        // EDT: Replaced VkSnapshot(int) with RegisteredKeys(Keys[])
        public static Keys[] RegisteredKeys;
        private Gamepad _gamePad;
        const int KfAltdown = 0x2000;
        public const int LlkhfAltdown = (KfAltdown >> 8);

        public IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)KeyboardState.KeyDown)
            {
                bool fEatKeyStroke = false;
                try
                {
                    var wparamTyped = wParam.ToInt32();

                    object o = Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));
                    LowLevelKeyboardInputEvent p = (LowLevelKeyboardInputEvent)o;
                    var eventArguments = new GlobalKeyboardHookEventArgs(p, (KeyboardState)wparamTyped);
                    // EDT: Removed the comparison-logic from the usage-area so the user does not need to mess around with it.
                    // Either the incoming key has to be part of RegisteredKeys (see constructor on top) or RegisterdKeys
                    // has to be null for the event to get fired.
                    var key = (Keys)p.VirtualCode;
                    if (RegisteredKeys == null || RegisteredKeys.Contains(key))
                    {
                        EventHandler<GlobalKeyboardHookEventArgs> handler = KeyboardPressed;
                        handler?.Invoke(this, eventArguments);

                        fEatKeyStroke = eventArguments.Handled;
                    }

                }
                catch
                {
                    // ignore
                }

                //return fEatKeyStroke ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            return CallNextHookEx(_windowsHookHandle, nCode, wParam, lParam);
        }

        private const int EXTRA_INFO = 1000;

        IntPtr extraInfoPointer = new IntPtr(EXTRA_INFO);

        public IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool fEatKeyStroke = false;
            try
            {
                var value = (MouseMessages)wParam;
                if (nCode >= 0)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    if (MouseMessages.WM_MOUSEMOVE != value)
                    {

                        if (MouseMessages.WM_LBUTTONDOWN == value)
                        {
                            Action<int> handler = MouseButtonPressed;
                            handler?.Invoke(0);
                        }
                        else if (MouseMessages.WM_RBUTTONDOWN == value)
                        {
                            Action<int> handler = MouseButtonPressed;
                            handler?.Invoke(1);

                        }
                        else if (MouseMessages.WM_BTN_MID == value)
                        {
                            Action<int> handler = MouseButtonPressed;
                            handler?.Invoke(2);

                        }
                        else if (hookStruct.mouseData == 131072)
                        {
                            Action<int> handler = MouseButtonPressed;
                            handler?.Invoke(3);
                        }
                        else if (hookStruct.mouseData == 65536)
                        {
                            Action<int> handler = MouseButtonPressed;
                            handler?.Invoke(4);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            //return fEatKeyStroke ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);

        }
    }
}
