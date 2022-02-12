using System;

namespace D2RSO.Classes
{
    public class GamepadInfo
    {
        public Guid InstanceGuid { get; private set; }
        public string InstanceName;
        public Guid ProductGuid;
        public string ProductName;
        public string DeviceType;
        public int SubType;

        public GamepadInfo(Guid guid)
        {
            InstanceGuid = guid;
        }
    }
}