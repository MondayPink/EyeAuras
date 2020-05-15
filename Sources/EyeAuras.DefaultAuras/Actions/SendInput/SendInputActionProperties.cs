using System;
using System.Drawing;
using System.Windows.Forms;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    public sealed class SendInputProperties : IAuraProperties
    {
        public TimeSpan WindowActivationTimeout { get; set; } = TimeSpan.FromSeconds(1);
        
        public TimeSpan KeyStrokeDelay { get; set; }
        
        public TimeSpan MaxKeyStrokeDelay { get; set; }
        
        public string Hotkey { get; set; } = Keys.None.ToString();
        
        public Point MouseLocation { get; set; }
        
        public WindowMatchParams TargetWindow { get; set; }
        
        public bool RestoreMousePosition { get; set; }
        
        public bool BlockUserInput { get; set; }
        
        public int Version { get; set; } = 1;
    }
}