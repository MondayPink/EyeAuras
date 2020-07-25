using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using EyeAuras.OnTopReplica.Native;
using log4net;
using Newtonsoft.Json;
using PInvoke;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace EyeAuras.OnTopReplica
{
    internal sealed class WindowHandle : IWindowHandle
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowHandle));

        private readonly Lazy<string> classSupplier;
        private readonly Lazy<Rectangle> windowBoundsSupplier;
        private readonly Lazy<Rectangle> clientBoundsSupplier;
        private readonly Lazy<Icon> iconSupplier;
        private readonly Lazy<BitmapSource> iconBitmapSupplier;
        private readonly Lazy<(string processName, string processPath)> processDataSupplier;
        
        public WindowHandle(IntPtr handle)
        {
            Handle = handle;
            Title = UnsafeNative.GetWindowTitle(handle);
            ProcessId = UnsafeNative.GetProcessIdByWindowHandle(handle);
            
            classSupplier = new Lazy<string>(() => UnsafeNative.GetWindowClass(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            windowBoundsSupplier = new Lazy<Rectangle>(() => UnsafeNative.GetWindowRect(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            clientBoundsSupplier = new Lazy<Rectangle>(() => UnsafeNative.GetClientRect(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            iconSupplier = new Lazy<Icon>(() => GetWindowIcon(handle), LazyThreadSafetyMode.ExecutionAndPublication);
            iconBitmapSupplier = new Lazy<BitmapSource>(() =>
            {
                try
                {
                    var icon = iconSupplier.Value;
                    var result = icon != null
                        ? Imaging.CreateBitmapSourceFromHIcon(Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                        : default;
                    result?.Freeze();
                    return result;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to get IconBitmap, window: {Title}, class: {Class}", ex);
                    return default;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            
            processDataSupplier = new Lazy<(string processName, string processPath)>(() =>
            {
                try
                {
                    var process = Process.GetProcessById(ProcessId);
                    var nativeProcessPath = UnsafeNative.QueryFullProcessImageName(ProcessId);
                    
                    string processName = nativeProcessPath != null ? Path.GetFileName(nativeProcessPath) : process.ProcessName;
                    string processPath;
                    try
                    {
                        processPath = process.MainModule?.FileName;
                    }
                    catch (Win32Exception)
                    {
                        processPath = nativeProcessPath;
                    }
                    
                    return (processName, processPath);
                }
                catch (Win32Exception)
                {
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to wrap Process with Id {ProcessId}, window: {Title}, class: {Class}", ex);
                }
                return default;
            });
        }
        
        public IntPtr Handle { get; }

        public string Title { get; }

        public int ProcessId { get; }

        public Rectangle WindowBounds => windowBoundsSupplier.Value;

        public Rectangle ClientBounds => clientBoundsSupplier.Value;

        [JsonIgnore] public Icon Icon => iconSupplier.Value;

        [JsonIgnore] public BitmapSource IconBitmap => iconBitmapSupplier.Value;

        public string Class => classSupplier.Value;

        public string ProcessPath => processDataSupplier.Value.processPath;

        public string ProcessName => processDataSupplier.Value.processName;
        
        public int ZOrder { get; set; }

        private static Icon GetWindowIcon(IntPtr handle)
        {
            if (MessagingMethods.SendMessageTimeout(
                    handle,
                    Wm.Geticon,
                    new IntPtr(0),
                    new IntPtr(0),
                    MessagingMethods.SendMessageTimeoutFlags.AbortIfHung | MessagingMethods.SendMessageTimeoutFlags.Block,
                    500,
                    out var hIcon) ==
                IntPtr.Zero)
            {
                hIcon = IntPtr.Zero;
            }

            Icon result = null;
            if (hIcon != IntPtr.Zero)
            {
                result = Icon.FromHandle(hIcon);
            }
            else
            {
                hIcon = WindowMethods.GetClassLong(handle, WindowMethods.ClassLong.Icon);

                if (hIcon.ToInt64() != 0)
                {
                    result = Icon.FromHandle(hIcon);
                }
            }

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Handle.ToHexadecimal());

            if (!string.IsNullOrWhiteSpace(Title))
            {
                sb.Append($" (title: {Title})");
            }

            if (!string.IsNullOrWhiteSpace(Class))
            {
                sb.Append($" (class: {Class})");
            }

            return sb.ToString();
        }

        public bool Equals(IWindowHandle other)
        {
            if (other == null)
            {
                return false;
            }
            return Handle.Equals(other.Handle);
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return Equals(other as WindowHandle);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public void Dispose()
        {
            if (iconSupplier?.IsValueCreated ?? false)
            {
                iconSupplier?.Value?.Dispose();
            }
        }
    }
}