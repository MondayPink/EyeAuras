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
        bool IsInverted { get; }

        bool IsActive { get; }
    }
}