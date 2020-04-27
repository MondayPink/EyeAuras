using System;
using System.Drawing;
using System.Windows.Forms;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    public sealed class SendInputProperties : IAuraProperties
    {
        public TimeSpan KeyStrokeDelay { get; set; }
        
        public string Hotkey { get; set; } = Keys.None.ToString();
        
        public Point MouseLocation { get; set; }
        
        public WindowMatchParams TargetWindow { get; set; }
        
        public bool RestoreMousePosition { get; set; }
        
        public bool BlockUserInput { get; set; }
        
        public int Version { get; set; } = 1;
    }
}