using System;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace EyeAuras.OnTopReplica
{
    public interface IWindowHandle : IWin32Window, IDisposable, IEquatable<IWindowHandle>
    {
        string Title { [CanBeNull] get; }
        
        Rectangle WindowBounds { get; }
        
        Rectangle ClientBounds { get; }
        
        Icon Icon { get; }
        
        BitmapSource IconBitmap { get; }
        
        string Class { get; }
        
        int ProcessId { get; }
        
        string ProcessPath { [CanBeNull] get; }
        
        string ProcessName { [CanBeNull] get; }
        
        int ZOrder { get; set; }
    }
}