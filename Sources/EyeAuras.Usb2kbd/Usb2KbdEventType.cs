namespace EyeAuras.Usb2kbd
{
    internal enum Usb2KbdEventType : int
    {
        KeyDown = 1,
        KeyUp = 2,
        MouseAbsolute = 3,
        MouseRelative = 4,
        MouseAbsoluteNoAcceleration = 5,
        MouseRelativeNoAcceleration = 6,
        MouseWheel = 7,
        AllKeysUp = 8,
    }
}