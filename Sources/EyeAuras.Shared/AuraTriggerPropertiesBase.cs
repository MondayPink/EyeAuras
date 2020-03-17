namespace EyeAuras.Shared
{
    public abstract class AuraTriggerPropertiesBase : IAuraTriggerProperties
    {
        public bool IsInverted { get; set; }

        public virtual int Version { get; set; } = 1;
    }
}