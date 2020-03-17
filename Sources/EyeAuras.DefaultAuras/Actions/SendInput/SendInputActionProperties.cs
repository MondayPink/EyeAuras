using System;
using System.Windows.Forms;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputProperties : IAuraProperties
    {
        public TimeSpan KeyStrokeDelay { get; set; }
        
        public string Hotkey { get; set; } = Keys.None.ToString();

        public int Version { get; set; } = 1;
    }
}