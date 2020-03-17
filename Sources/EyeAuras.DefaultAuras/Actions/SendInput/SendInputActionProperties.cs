using System;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputProperties : IAuraProperties
    {
        public TimeSpan KeyStrokeDelay { get; set; }

        public int Version { get; set; } = 1;
    }
}