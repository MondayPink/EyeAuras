namespace EyeAuras.Shared
{
    public abstract class AuraTriggerPropertiesBase : IAuraTriggerProperties
    {
        public bool IsInverted { get; set; }
        
        public abstract int Version { get; set; }
    }
}