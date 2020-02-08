using EyeAuras.Shared;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTriggerProperties : IAuraProperties
    {
        public bool IsInverted { get; set; }
        
        public string AuraId { get; set; }
        
        public int Version { get; set; } = 1;
    }
}