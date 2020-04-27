using System;
using System.Drawing;
using EyeAuras.Shared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputActionEditor : AuraPropertiesEditorBase<SendInputAction>
    {
        public SendInputActionEditor()
        {
            this.WhenAnyValue(x => x.Source.KeyStrokeDelay)
                .Subscribe(() => RaisePropertyChanged(nameof(KeyStrokeDelay)))
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Source.MouseLocation)
                .Subscribe(
                    () =>
                    {
                        RaisePropertyChanged(nameof(MouseX));
                        RaisePropertyChanged(nameof(MouseY));
                    })
                .AddTo(Anchors);
        }

        public int KeyStrokeDelay
        {
            get => (int)Source.KeyStrokeDelay.TotalMilliseconds;
            set => Source.KeyStrokeDelay = TimeSpan.FromMilliseconds(value);
        }

        public int MouseX
        {
            get => (int)Source.MouseLocation.X;
            set => Source.MouseLocation = new Point(value, Source.MouseLocation.Y);
        }

        public int MouseY
        {
            get => (int)Source.MouseLocation.Y;
            set => Source.MouseLocation = new Point(Source.MouseLocation.X, value);
        }
    }
}