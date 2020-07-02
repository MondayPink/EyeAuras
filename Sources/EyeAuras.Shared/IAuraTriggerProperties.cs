namespace EyeAuras.Shared
{
    public interface IAuraTriggerProperties : IAuraProperties
    {
        bool IsInverted { get; set; }
    }
}