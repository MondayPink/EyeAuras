using System;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Actions.WinActivate
{
    public sealed class WinActivateActionProperties : IAuraProperties
    {
        public WindowMatchParams WindowMatchParams { get; set; }
        
        public TimeSpan Timeout { get; set; }
        
        public int Version { get; set; } = 1;
    }
}