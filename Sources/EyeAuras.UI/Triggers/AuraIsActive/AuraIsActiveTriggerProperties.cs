using System;
using EyeAuras.Shared;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTriggerProperties : AuraTriggerPropertiesBase
    {
        public string AuraId { get; set; }
        
        public override int Version { get; set; } = 1;
        
        public TimeSpan ActivationTimeout { get; set; }
    }
}