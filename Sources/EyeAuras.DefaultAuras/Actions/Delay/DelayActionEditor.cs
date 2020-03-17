using System;
using EyeAuras.DefaultAuras.Actions.PlaySound;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.Delay
{
    internal sealed class DelayActionEditor : AuraPropertiesEditorBase<DelayAction>
    {
        public int Delay
        {
            get => (int)Source.Delay.TotalMilliseconds;
            set => Source.Delay = TimeSpan.FromMilliseconds(value);
        }
    }
}