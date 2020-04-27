using System.Linq;
using WindowsInput.Native;
using EyeAuras.Interception;
using EyeAuras.Usb2kbd;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace EyeAuras.Tests.Interception
{
    [TestFixture]
    public class VirtualKeyCodeToScanCodeConverterFixture
    {
        private static VirtualKeyCode[] ExcludedKeyCodes = new VirtualKeyCode[]
        {
            VirtualKeyCode.LBUTTON,
            VirtualKeyCode.MBUTTON,
            VirtualKeyCode.RBUTTON,
            VirtualKeyCode.XBUTTON1,
            VirtualKeyCode.XBUTTON2,
            VirtualKeyCode.ACCEPT,
            VirtualKeyCode.ATTN,
            VirtualKeyCode.CONVERT,
            VirtualKeyCode.CRSEL,
            VirtualKeyCode.EXECUTE,
            VirtualKeyCode.EXSEL,
            VirtualKeyCode.FINAL,
            VirtualKeyCode.HANGUL,
            VirtualKeyCode.HANJA,
            VirtualKeyCode.MODECHANGE,
            VirtualKeyCode.HANGEUL,
            VirtualKeyCode.JUNJA,
            VirtualKeyCode.NONAME,
            VirtualKeyCode.NONCONVERT,
            VirtualKeyCode.OEM_CLEAR,
            VirtualKeyCode.PA1,
            VirtualKeyCode.PACKET,
            VirtualKeyCode.PROCESSKEY,
            VirtualKeyCode.PLAY,
            VirtualKeyCode.PRINT,
            VirtualKeyCode.SEPARATOR,
            VirtualKeyCode.SELECT,
            VirtualKeyCode.PAUSE,
            VirtualKeyCode.OEM_8,
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
                result.ShouldBe((uint)0);
            }
            else
            {
                result.ShouldNotBe((uint)0);
            }
        }
        
        [Test]
        [TestCase(VirtualKeyCode.VK_0, 0x0b)]
        [TestCase(VirtualKeyCode.VK_A, 0x1e)]
        [TestCase(VirtualKeyCode.VK_Z, 0x2c)]
        [TestCase(VirtualKeyCode.NUMPAD0, 0x52)]
        [TestCase(VirtualKeyCode.F1, 0x3b)]
        [TestCase(VirtualKeyCode.F24, 0x76)]
        [TestCase(VirtualKeyCode.SPACE, 0x39)]
        public void ShouldConvertKeys(VirtualKeyCode keyCode, int expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(keyCode);

            //Then
            result.ShouldBe((uint)expected, () => $"result: {result.ToHexadecimal()}, expected: {expected.ToHexadecimal()}");
        }
        
        public VirtualKeyCodeToScanCodeConverter CreateInstance()
        {
            return new VirtualKeyCodeToScanCodeConverter();
        }
    }
}