using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Triggers.Default
{
    public sealed class DefaultTriggerProperties : AuraTriggerPropertiesBase
    {
        public bool TriggerValue { get; set; } = true;
        
        public override int Version { get; set; } = 1;
    }
}