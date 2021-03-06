using EyeAuras.Shared;
using EyeAuras.Shared.Services;

namespace EyeAuras.DefaultAuras.Triggers.WinActive
{
    internal sealed class WinActiveTriggerProperties : AuraTriggerPropertiesBase
    {
        public WindowMatchParams WindowMatchParams { get; set; }
        public override int Version { get; set; } = 1;
    }
}