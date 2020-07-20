using System.Drawing;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.OnTopReplica
{
    public sealed class EditableRectangleF : DisposableReactiveObject
    {
        private readonly ObservableAsPropertyHelper<RectangleF> bounds;

        private float x;
        private float y;
        private float width;
        private float height;
        
        public EditableRectangleF()
        {
            bounds = this.WhenAnyProperty(x => x.X, x => x.Y, x => x.Width, x => x.Height)
                .Select(() => new RectangleF(x, y, width, height))
                .ToPropertyHelper(this, x => x.Bounds)
                .AddTo(Anchors);
        }
        
        public RectangleF Bounds => bounds.Value;

        public float X
        {
            get => x;
            set => RaiseAndSetIfChanged(ref x, value);
        }

        public float Y
        {
            get => y;
            set => RaiseAndSetIfChanged(ref y, value);
        }

        public float Width
        {
            get => width;
            set => RaiseAndSetIfChanged(ref width, value);
        }

        public float Height
        {
            get => height;
            set => RaiseAndSetIfChanged(ref height, value);
        }
    }
}