using System;

namespace EyeAuras.Shared
{
    public interface IAuraTrigger : IAuraModel
    {
        string TriggerName { get; }

        string TriggerDescription { get; }

        /// <summary>
        ///    Indicates whether IsActive should be Inverted or not for the final result
        ///    Respected by ComplexAuraTrigger
        /// </summary>
        bool IsInverted { get; set; }

        bool IsActive { get; }
        
        TimeSpan? TimeLeftTillNextActivation { get; }

        TimeSpan ActivationTimeout { get; set; }
        
        double ActivationProgress { get; }

        bool NextIsActiveValue { get; }
        
        bool EnableAdvancedSettings { get; set; }
    }
}