using System;
using System.Linq;
using System.Timers;
using SharpDX.DirectInput;

namespace D2RSO.Classes
{
    public class Gamepad: IDisposable
    {
        public GamepadInfo gamepadInfo;

        Timer timer;
        DirectInput directInput;
        Guid joystickGuid = Guid.Empty;
        Joystick joystick;

        const int TIMER_INTERVAL_IN_MS = 10;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buttonNo"></param>
        /// <param name="pressed">
        /// pressed = 1
        /// unpressed = 0;
        /// </param>
        public delegate void newGamePadButtonInfoAcquiredEventHandler(object sender, string btn, bool pressed);

        public event newGamePadButtonInfoAcquiredEventHandler evNewGamePadButtonInfoAcquired;

        public Gamepad()
        {
            directInput = new DirectInput();

            // Find Joystick/Gamepad Guids
            foreach (var deviceInstance in directInput.GetDevices()
                         .Where(x => x.Type == DeviceType.Gamepad || x.Type == DeviceType.Joystick))
            {
                joystickGuid = deviceInstance.InstanceGuid;
            }

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No joystick/Gamepad found.");
                return;
            }

            // Instantiate the joystick
            joystick = new Joystick(directInput, joystickGuid);

            // Get and set gamepadInfo
            var info = joystick.Information;
            gamepadInfo = new GamepadInfo(info.InstanceGuid)
            {
                InstanceName = info.InstanceName,
                ProductGuid = info.ProductGuid,
                ProductName = info.ProductName,
                DeviceType = info.Type.ToString(),
                SubType = info.Subtype
            };

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();


            timer = new Timer {Interval = TIMER_INTERVAL_IN_MS};
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            joystick.Poll();
            var datas = joystick.GetBufferedData();
            foreach (var state in datas)
            {
                if (((int) state.Offset) >= 48 && ((int) state.Offset) <= 175)
                {
                    newGamePadButtonInfoAcquiredEventHandler temp = evNewGamePadButtonInfoAcquired;
                    if (temp != null)
                    {
                        if (state.Value == 0)
                        {
                            temp(this, state.Offset.ToString(), false);
                        }
                        else
                        {
                            temp(this, state.Offset.ToString(), true);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            timer?.Stop();

            joystick?.Unacquire();
            directInput?.Dispose();

            timer?.Close();
        }
    }
}