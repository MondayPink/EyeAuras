using System;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Triggers.Timer
{
    public sealed class TimerTriggerProperties : AuraTriggerPropertiesBase
    {
        public TimeSpan ActivationPeriod { get; set; }
        
        public TimeSpan DeactivationTimeout { get; set; }
    }
}