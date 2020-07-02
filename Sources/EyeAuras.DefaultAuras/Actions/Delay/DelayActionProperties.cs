using System;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.Delay
{
    public sealed class DelayActionProperties : IAuraProperties
    {
        public TimeSpan Delay { get; set; }

        public int Version { get; set; } = 1;
    }
}