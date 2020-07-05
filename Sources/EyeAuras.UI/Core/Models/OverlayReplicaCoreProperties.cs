using System;
using System.Drawing;
using EyeAuras.Shared.Services;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayReplicaCoreProperties : OverlayCoreProperties
    {
        public WindowMatchParams WindowMatch { get; set; }

        public Rectangle SourceRegionBounds { get; set; }
        
        public override int Version { get; set; } = 1;
    }
}