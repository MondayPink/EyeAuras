using System.Collections.Generic;
using WindowsInput.Native;
using PoeShared.Prism;

namespace EyeAuras.Usb2kbd
{
    internal sealed class KeyToUsbHidScanCodeConverter : IConverter<VirtualKeyCode, UsbHidScanCodes>
    {
        private readonly Dictionary<VirtualKeyCode, UsbHidScanCodes> keyToScanCode = new Dictionary<VirtualKeyCode, UsbHidScanCodes>
        {
            {VirtualKeyCode.VK_A, UsbHidScanCodes.KEY_A},
            {VirtualKeyCode.VK_B, UsbHidScanCodes.KEY_B},
            {VirtualKeyCode.VK_C, UsbHidScanCodes.KEY_C},
            {VirtualKeyCode.VK_D, UsbHidScanCodes.KEY_D},
            {VirtualKeyCode.VK_E, UsbHidScanCodes.KEY_E},
            {VirtualKeyCode.VK_F, UsbHidScanCodes.KEY_F},
            {VirtualKeyCode.VK_G, UsbHidScanCodes.KEY_G},
            {VirtualKeyCode.VK_H, UsbHidScanCodes.KEY_H},
            {VirtualKeyCode.VK_I, UsbHidScanCodes.KEY_I},
            {VirtualKeyCode.VK_J, UsbHidScanCodes.KEY_J},
            {VirtualKeyCode.VK_K, UsbHidScanCodes.KEY_K},
            {VirtualKeyCode.VK_L, UsbHidScanCodes.KEY_L},
            {VirtualKeyCode.VK_M, UsbHidScanCodes.KEY_M},
            {VirtualKeyCode.VK_N, UsbHidScanCodes.KEY_N},
            {VirtualKeyCode.VK_O, UsbHidScanCodes.KEY_O},
            {VirtualKeyCode.VK_P, UsbHidScanCodes.KEY_P},
            {VirtualKeyCode.VK_Q, UsbHidScanCodes.KEY_Q},
            {VirtualKeyCode.VK_R, UsbHidScanCodes.KEY_R},
            {VirtualKeyCode.VK_S, UsbHidScanCodes.KEY_S},
            {VirtualKeyCode.VK_T, UsbHidScanCodes.KEY_T},
            {VirtualKeyCode.VK_U, UsbHidScanCodes.KEY_U},
            {VirtualKeyCode.VK_V, UsbHidScanCodes.KEY_V},
            {VirtualKeyCode.VK_W, UsbHidScanCodes.KEY_W},
            {VirtualKeyCode.VK_X, UsbHidScanCodes.KEY_X},
            {VirtualKeyCode.VK_Y, UsbHidScanCodes.KEY_Y},
            {VirtualKeyCode.VK_Z, UsbHidScanCodes.KEY_Z},
            {VirtualKeyCode.VK_1, UsbHidScanCodes.KEY_1},
            {VirtualKeyCode.VK_2, UsbHidScanCodes.KEY_2},
            {VirtualKeyCode.VK_3, UsbHidScanCodes.KEY_3},
            {VirtualKeyCode.VK_4, UsbHidScanCodes.KEY_4},
            {VirtualKeyCode.VK_5, UsbHidScanCodes.KEY_5},
            {VirtualKeyCode.VK_6, UsbHidScanCodes.KEY_6},
            {VirtualKeyCode.VK_7, UsbHidScanCodes.KEY_7},
            {VirtualKeyCode.VK_8, UsbHidScanCodes.KEY_8},
            {VirtualKeyCode.VK_9, UsbHidScanCodes.KEY_9},
            {VirtualKeyCode.VK_0, UsbHidScanCodes.KEY_0},
            {VirtualKeyCode.RETURN, UsbHidScanCodes.KEY_ENTER},
            {VirtualKeyCode.ESCAPE, UsbHidScanCodes.KEY_ESC},
            {VirtualKeyCode.BACK, UsbHidScanCodes.KEY_BACKSPACE},
            {VirtualKeyCode.TAB, UsbHidScanCodes.KEY_TAB},
            {VirtualKeyCode.SPACE, UsbHidScanCodes.KEY_SPACE},
            {VirtualKeyCode.OEM_MINUS, UsbHidScanCodes.KEY_MINUS},
            {VirtualKeyCode.F1, UsbHidScanCodes.KEY_F1},
            {VirtualKeyCode.F2, UsbHidScanCodes.KEY_F2},
            {VirtualKeyCode.F3, UsbHidScanCodes.KEY_F3},
            {VirtualKeyCode.F4, UsbHidScanCodes.KEY_F4},
            {VirtualKeyCode.F5, UsbHidScanCodes.KEY_F5},
            {VirtualKeyCode.F6, UsbHidScanCodes.KEY_F6},
            {VirtualKeyCode.F7, UsbHidScanCodes.KEY_F7},
            {VirtualKeyCode.F8, UsbHidScanCodes.KEY_F8},
            {VirtualKeyCode.F9, UsbHidScanCodes.KEY_F9},
            {VirtualKeyCode.F10, UsbHidScanCodes.KEY_F10},
            {VirtualKeyCode.F11, UsbHidScanCodes.KEY_F11},
            {VirtualKeyCode.F12, UsbHidScanCodes.KEY_F12},
            {VirtualKeyCode.SCROLL, UsbHidScanCodes.KEY_SCROLLLOCK},
            {VirtualKeyCode.PAUSE, UsbHidScanCodes.KEY_PAUSE},
            {VirtualKeyCode.INSERT, UsbHidScanCodes.KEY_INSERT},
            {VirtualKeyCode.HOME, UsbHidScanCodes.KEY_HOME},
            {VirtualKeyCode.PRIOR, UsbHidScanCodes.KEY_PAGEUP},
            {VirtualKeyCode.DELETE, UsbHidScanCodes.KEY_DELETE},
            {VirtualKeyCode.END, UsbHidScanCodes.KEY_END},
            {VirtualKeyCode.NEXT, UsbHidScanCodes.KEY_PAGEDOWN},
            {VirtualKeyCode.RIGHT, UsbHidScanCodes.KEY_RIGHT},
            {VirtualKeyCode.LEFT, UsbHidScanCodes.KEY_LEFT},
            {VirtualKeyCode.DOWN, UsbHidScanCodes.KEY_DOWN},
            {VirtualKeyCode.UP, UsbHidScanCodes.KEY_UP},
            {VirtualKeyCode.NUMLOCK, UsbHidScanCodes.KEY_NUMLOCK},
            {VirtualKeyCode.NUMPAD1, UsbHidScanCodes.KEY_KP1},
            {VirtualKeyCode.NUMPAD2, UsbHidScanCodes.KEY_KP2},
            {VirtualKeyCode.NUMPAD3, UsbHidScanCodes.KEY_KP3},
            {VirtualKeyCode.NUMPAD4, UsbHidScanCodes.KEY_KP4},
            {VirtualKeyCode.NUMPAD5, UsbHidScanCodes.KEY_KP5},
            {VirtualKeyCode.NUMPAD6, UsbHidScanCodes.KEY_KP6},
            {VirtualKeyCode.NUMPAD7, UsbHidScanCodes.KEY_KP7},
            {VirtualKeyCode.NUMPAD8, UsbHidScanCodes.KEY_KP8},
            {VirtualKeyCode.NUMPAD9, UsbHidScanCodes.KEY_KP9},
            {VirtualKeyCode.NUMPAD0, UsbHidScanCodes.KEY_KP0},
            {VirtualKeyCode.VOLUME_UP, UsbHidScanCodes.KEY_VOLUMEUP},
            {VirtualKeyCode.VOLUME_DOWN, UsbHidScanCodes.KEY_VOLUMEDOWN},
            {VirtualKeyCode.LCONTROL, UsbHidScanCodes.KEY_LEFTCTRL},
            {VirtualKeyCode.LSHIFT, UsbHidScanCodes.KEY_LEFTSHIFT},
            {VirtualKeyCode.LMENU, UsbHidScanCodes.KEY_LEFTALT},
            {VirtualKeyCode.RCONTROL, UsbHidScanCodes.KEY_RIGHTCTRL},
            {VirtualKeyCode.RSHIFT, UsbHidScanCodes.KEY_RIGHTSHIFT},
            {VirtualKeyCode.RMENU, UsbHidScanCodes.KEY_RIGHTALT}
        };

        public UsbHidScanCodes Convert(VirtualKeyCode value)
        {
            if (keyToScanCode.TryGetValue(value, out var result))
            {
                return result;
            }

            return UsbHidScanCodes.KEY_NONE;
        }
    }
}