using System;
using EyeAuras.Shared;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputActionEditor : AuraPropertiesEditorBase<SendInputAction>
    {
        public int KeyStrokeDelay
        {
            get => (int)Source.KeyStrokeDelay.TotalMilliseconds;
            set => Source.KeyStrokeDelay = TimeSpan.FromMilliseconds(value);
        }
    }
}