namespace EyeAuras.Usb2kbd
{
    internal enum Usb2KbdEventType : int
    {
        KeyDown = 1,
        KeyUp = 2,
        MouseAbsoluteDisableAcceleration = 3,
        MouseRelativeDisableAcceleration = 4,
        MouseAbsolute = 5,
        MouseRelative = 6,
        MouseWheel = 7,
        AllKeysUp = 8,
    }
}