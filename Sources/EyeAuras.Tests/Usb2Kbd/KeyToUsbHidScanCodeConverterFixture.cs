using System.Linq;
using WindowsInput.Native;
using EyeAuras.Usb2kbd;
using NUnit.Framework;
using Shouldly;

namespace EyeAuras.Tests.Usb2Kbd
{
    [TestFixture]
    internal class KeyToUsbHidScanCodeConverterFixture
    {
        private static VirtualKeyCode[] ExcludedKeyCodes = new VirtualKeyCode[]
        {
            VirtualKeyCode.LBUTTON,
            VirtualKeyCode.MBUTTON,
            VirtualKeyCode.RBUTTON,
            VirtualKeyCode.XBUTTON1,
            VirtualKeyCode.XBUTTON2,
            VirtualKeyCode.ACCEPT,
            VirtualKeyCode.APPS,
            VirtualKeyCode.ATTN,
            VirtualKeyCode.BROWSER_FAVORITES,
            VirtualKeyCode.CONVERT,
            VirtualKeyCode.CLEAR,
            VirtualKeyCode.CRSEL,
            VirtualKeyCode.EREOF,
            VirtualKeyCode.EXECUTE,
            VirtualKeyCode.EXSEL,
            VirtualKeyCode.FINAL,
            VirtualKeyCode.HANGUL,
            VirtualKeyCode.HANJA,
            VirtualKeyCode.MODECHANGE,
            VirtualKeyCode.HANGEUL,
            VirtualKeyCode.JUNJA,
            VirtualKeyCode.LAUNCH_APP1,
            VirtualKeyCode.LAUNCH_APP2,
            VirtualKeyCode.LAUNCH_MAIL,
            VirtualKeyCode.LAUNCH_MEDIA_SELECT,
            VirtualKeyCode.NONAME,
            VirtualKeyCode.NONCONVERT,
            VirtualKeyCode.OEM_CLEAR,
            VirtualKeyCode.OEM_COMMA,
            VirtualKeyCode.OEM_PERIOD,
            VirtualKeyCode.OEM_102,
            VirtualKeyCode.OEM_1,
            VirtualKeyCode.OEM_2,
            VirtualKeyCode.OEM_3,
            VirtualKeyCode.OEM_4,
            VirtualKeyCode.OEM_5,
            VirtualKeyCode.OEM_6,
            VirtualKeyCode.OEM_7,
            VirtualKeyCode.OEM_8,
            VirtualKeyCode.PA1,
            VirtualKeyCode.PACKET,
            VirtualKeyCode.PROCESSKEY,
            VirtualKeyCode.PLAY,
            VirtualKeyCode.PRINT,
            VirtualKeyCode.SEPARATOR,
            VirtualKeyCode.SELECT,
            VirtualKeyCode.ZOOM,
        };
        
        [Test]
        public void ShouldConvertAllKeys([Values] VirtualKeyCode keyCode)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(keyCode);

            //Then
            if (ExcludedKeyCodes.Contains(keyCode))
            {
                result.ShouldBe(UsbHidScanCodes.KEY_NONE);
            }
            else
            {
                result.ShouldNotBe(UsbHidScanCodes.KEY_NONE);
            }
        }

        [Test]
        [TestCase(VirtualKeyCode.VK_A, UsbHidScanCodes.KEY_A)]
        [TestCase(VirtualKeyCode.VK_0, UsbHidScanCodes.KEY_0)]
        [TestCase(VirtualKeyCode.VK_9, UsbHidScanCodes.KEY_9)]
        [TestCase(VirtualKeyCode.OEM_MINUS, UsbHidScanCodes.KEY_MINUS)]
        [TestCase(VirtualKeyCode.OEM_PLUS, UsbHidScanCodes.KEY_EQUAL)]
        [TestCase(VirtualKeyCode.F1, UsbHidScanCodes.KEY_F1)]
        [TestCase(VirtualKeyCode.F24, UsbHidScanCodes.KEY_F24)]
        [TestCase(VirtualKeyCode.SUBTRACT, UsbHidScanCodes.KEY_KPMINUS)]
        [TestCase(VirtualKeyCode.LBUTTON, UsbHidScanCodes.KEY_NONE)]
        [TestCase(VirtualKeyCode.RBUTTON, UsbHidScanCodes.KEY_NONE)]
        [TestCase(VirtualKeyCode.MBUTTON, UsbHidScanCodes.KEY_NONE)]
        [TestCase(VirtualKeyCode.XBUTTON1, UsbHidScanCodes.KEY_NONE)]
        [TestCase(VirtualKeyCode.XBUTTON2, UsbHidScanCodes.KEY_NONE)]
        public void ShouldConvertKeys(VirtualKeyCode keyCode, UsbHidScanCodes expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(keyCode);

            //Then
            result.ShouldBe(expected);
        }
        
        public KeyToUsbHidScanCodeConverter CreateInstance()
        {
            return new KeyToUsbHidScanCodeConverter();
        }
    }
}