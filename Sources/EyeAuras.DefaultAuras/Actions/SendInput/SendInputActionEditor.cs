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
            this.WhenAnyValue(x => x.Source.MinKeyStrokeDelay)
                .Subscribe(() => RaisePropertyChanged(nameof(MinKeyStrokeDelay)))
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Source.MaxKeyStrokeDelay)
                .Subscribe(() => RaisePropertyChanged(nameof(MaxKeyStrokeDelay)))
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

        public double SystemMinKeyStrokeDelay => Source.SystemMinKeyStrokeDelay.TotalMilliseconds;
        
        public double SystemMaxKeyStrokeDelay => Source.SystemMaxKeyStrokeDelay.TotalMilliseconds;

        public int MinKeyStrokeDelay
        {
            get => (int)Source.MinKeyStrokeDelay.TotalMilliseconds;
            set => Source.MinKeyStrokeDelay = TimeSpan.FromMilliseconds(value);
        }
        
        public int MaxKeyStrokeDelay
        {
            get => (int)Source.MaxKeyStrokeDelay.TotalMilliseconds;
            set => Source.MaxKeyStrokeDelay = TimeSpan.FromMilliseconds(value);
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