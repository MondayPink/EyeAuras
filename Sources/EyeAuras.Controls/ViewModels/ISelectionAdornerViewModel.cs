using System;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Controls.ViewModels
{
    public interface ISelectionAdornerViewModel : IDisposableReactiveObject
    {
        double StrokeThickness { get; }
        
        Brush Stroke { [CanBeNull] get; }
        
        Point AnchorPoint { get; }
        
        Size Size { get; }
        
        Point MousePosition { get; }
        
        bool StopWhenFocusLost { get; set; }
        
        UIElement Owner { [CanBeNull] get; }
        
        [NotNull]
        IObservable<Rect> StartSelection();
    }
}