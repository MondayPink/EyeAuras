using PoeShared.Modularity;

namespace EyeAuras.Usb2kbd
{
    internal sealed class Usb2KbdConfig : IPoeEyeConfigVersioned
    {
        public string MethodName { get; set; }
        
        public string DeviceId { get; set; }
        
        public int SerialNumber { get; set; }

        public int KeyPressDelay { get; set; } = 100;
        
        public int Version { get; set; } = 1;
    }
}