namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayImageCoreProperties : OverlayCoreProperties
    {
        public string ImageFilePath { get; set; }
        
        public override int Version { get; set; } = 1;
    }
}