using System;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EyeAuras.OnTopReplica;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;
using Color = System.Windows.Media.Color;
using Size = System.Drawing.Size;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal abstract class EyeOverlayViewModel : OverlayViewModelBase, IEyeOverlayViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeOverlayViewModel));
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly double DefaultThumbnailOpacity = 1;
        private static readonly double EditModeThumbnailOpacity = 0.7;

        private readonly CommandWrapper fitOverlayCommand;
        private readonly IWindowTracker mainWindowTracker;
        private readonly IAuraModelController auraModelController;

        private readonly CommandWrapper setClickThroughCommand;
        private readonly Fallback<double?> thumbnailOpacity = new Fallback<double?>();

        private readonly ObservableAsPropertyHelper<bool> isInEditMode;
        private readonly ObservableAsPropertyHelper<double> aspectRatio;

        private bool maintainAspectRatio = true;
        private Size sourceWindowSize;
        private DpiScale dpi;
        private bool isClickThrough;
        private bool isInSelectMode;
        private string overlayName;
        private Size thumbnailSize;
        private Color borderColor;
        private double borderThickness;

        public EyeOverlayViewModel(
            [NotNull] [Dependency(WellKnownWindows.AllWindows)] IWindowTracker mainWindowTracker,
            [NotNull] IAuraModelController auraModelController,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            using var sw = new BenchmarkTimer("Initialization", Log, nameof(ReplicaOverlayViewModel));
            this.mainWindowTracker = mainWindowTracker;
            this.auraModelController = auraModelController;
            MinSize = new System.Windows.Size(32, 32);
            SizeToContent = SizeToContent.Manual;
            Width = 400;
            Height = 400;
            Left = 200;
            Top = 200;
            IsUnlockable = true;
            Title = "EyeAuras";
            EnableHeader = false;
            thumbnailOpacity.SetDefaultValue(DefaultThumbnailOpacity);

            sw.Step("Basic properties initialized");

            Disposable.Create(() => Log.Debug($"Overlay {OverlayName} is disposed")).AddTo(Anchors);
            
            WhenLoaded
                .Take(1)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);
            sw.Step("WhenLoaded executed");
            
            fitOverlayCommand = CommandWrapper.Create<double?>(FitOverlayCommandExecuted);
            setClickThroughCommand = CommandWrapper.Create<bool?>(SetClickThroughModeExecuted);
            DisableAuraCommand = CommandWrapper.Create(() => auraModelController.IsEnabled = false);
            CloseCommand = CommandWrapper.Create(CloseCommandExecuted, auraModelController.WhenAnyValue(x => x.CloseController).Select(CloseCommandCanExecute));
            ToggleLockStateCommand = CommandWrapper.Create(
                () =>
                {
                    if (IsLocked && UnlockWindowCommand.CanExecute(null))
                    {
                        UnlockWindowCommand.Execute(null);
                    }
                    else if (!IsLocked && LockWindowCommand.CanExecute(null))
                    {
                        LockWindowCommand.Execute(null);
                    }
                    else
                    {
                        throw new ApplicationException($"Something went wrong - invalid Overlay Lock state: {new {IsLocked, IsUnlockable, CanUnlock = UnlockWindowCommand.CanExecute(null), CanLock = LockWindowCommand.CanExecute(null)  }}");
                    }
                });

            auraModelController.WhenAnyValue(x => x.Name)
                .Where(x => !string.IsNullOrEmpty(x))
                .Subscribe(x => OverlayName = x)
                .AddTo(Anchors);

            this.RaiseWhenSourceValue(x => x.ActiveThumbnailOpacity, thumbnailOpacity, x => x.Value).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.ThumbnailOpacity, this, x => x.ActiveThumbnailOpacity).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.SourceBounds, Region, x => x.Bounds).AddTo(Anchors);

            isInEditMode = Observable.Merge(
                    this.WhenAnyProperty(x => x.IsInSelectMode, x => x.IsLocked))
                .Select(change => IsInSelectMode || !IsLocked)
                .ToPropertyHelper(this, x => x.IsInEditMode, uiScheduler)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsLocked)
                .Where(x => x && isInSelectMode)
                .Subscribe(() => IsInSelectMode = false)
                .AddTo(Anchors);

            aspectRatio = this.WhenAnyProperty(x => x.Bounds, x => x.ViewModelLocation)
                .Select(change => Width >= 0 && Height >= 0
                    ? Width / Height
                    : double.PositiveInfinity)
                .ToPropertyHelper(this, x => x.AspectRatio, uiScheduler)
                .AddTo(Anchors);
            sw.ResetStep();
        }

        private bool CloseCommandCanExecute()
        {
            return auraModelController.CloseController != null;
        }

        private void CloseCommandExecuted()
        {
            Guard.ArgumentIsTrue(CloseCommandCanExecute(), "CloseCommand can execute");
            
            Log.Debug($"Closing Overlay {OverlayName}");
            auraModelController.CloseController.Close();
        }

        public bool IsInEditMode => isInEditMode.Value;
        
        public bool IsInSelectMode
        {
            get => isInSelectMode;
            set => RaiseAndSetIfChanged(ref isInSelectMode, value);
        }

        public Color BorderColor
        {
            get => borderColor;
            set => RaiseAndSetIfChanged(ref borderColor, value);
        }

        public double BorderThickness
        {
            get => borderThickness;
            set => RaiseAndSetIfChanged(ref borderThickness, value);
        }

        public ICommand ToggleLockStateCommand { get; }

        public ICommand SetClickThroughCommand => setClickThroughCommand;

        public ICommand DisableAuraCommand { get; }
        
        public ICommand CloseCommand { get; }
        
        public double? ScaleRatioHalf => 1 / 2f;

        public double? ScaleRatioQuarter => 1 / 4f;

        public double? ScaleRatioActual => 1f;

        public double? ScaleRatioDouble => 2f;

        public double ActiveThumbnailOpacity => thumbnailOpacity.Value ?? DefaultThumbnailOpacity;

        public Size ThumbnailSize
        {
            get => thumbnailSize;
            set => RaiseAndSetIfChanged(ref thumbnailSize, value);
        }

        public Size SourceWindowSize
        {
            get => sourceWindowSize;
            set => RaiseAndSetIfChanged(ref sourceWindowSize, value);
        }

        public double AspectRatio => aspectRatio.Value;

        public ICommand FitOverlayCommand => fitOverlayCommand;

        public bool MaintainAspectRatio
        {
            get => maintainAspectRatio;
            set => RaiseAndSetIfChanged(ref maintainAspectRatio, value);
        }

        public DpiScale Dpi
        {
            get => dpi;
            private set => RaiseAndSetIfChanged(ref dpi, value);
        }

        public void ScaleOverlay(double scaleRatio)
        {
            if ( !ThumbnailSize.IsNotEmpty())
            {
                throw new InvalidOperationException("ThumbnailSize must be defined before Scaling");
            }
            var currentSize = new System.Windows.Size(Width, Height);
            var newSize = new System.Windows.Size(
                ThumbnailSize.Width * scaleRatio,
                ThumbnailSize.Height * scaleRatio
            ).Scale(1f / dpi.DpiScaleX, 1f / dpi.DpiScaleY);

            Log.Debug($"ScaleOverlay({scaleRatio}) executed, sizing args: {new {currentSize, newSize, thumbnailSize, dpi}}");

            Width = newSize.Width;
            Height = newSize.Height;
            
            Log.Debug($"ScaleOverlay({scaleRatio}) resized window, bounds: {Bounds} (native: {NativeBounds})");
        }

        public abstract bool IsInitialized { get; }

        public double ThumbnailOpacity
        {
            get => thumbnailOpacity.DefaultValue ?? DefaultThumbnailOpacity;
            set => thumbnailOpacity.SetDefaultValue(value);
        }

        public string OverlayName
        {
            get => overlayName;
            private set => RaiseAndSetIfChanged(ref overlayName, value);
        }

        public bool IsClickThrough
        {
            get => isClickThrough;
            set => RaiseAndSetIfChanged(ref isClickThrough, value);
        }

        public ThumbnailRegion Region { get; } = new ThumbnailRegion(Rectangle.Empty);

        public Rectangle SourceBounds
        {
            get => Region.Bounds;
            set => Region.SetValue(value);
        }

        private void SetClickThroughModeExecuted(bool? value)
        {
            IsClickThrough = value ?? false;
        }

        private void FitOverlayCommandExecuted(double? scaleRatio)
        {
            if (scaleRatio == null || !ThumbnailSize.IsNotEmpty())
            {
                return;
            }
            ScaleOverlay(scaleRatio.Value);
        }

        private void ApplyConfig()
        {
            dpi = VisualTreeHelper.GetDpi(OverlayWindow);

            mainWindowTracker.WhenAnyValue(x => x.ActiveProcessId)
                .ToUnit()
                .Merge(this.WhenAnyProperty(x => x.IsInSelectMode, x => x.IsLocked, x => x.IsClickThrough).ToUnit())
                .Select(
                    x => new
                    {
                        IsUnclocked = !IsLocked,
                        IsSelectedInMainWindow = mainWindowTracker.ActiveProcessId == CurrentProcessId,
                        IsNotClickThroughByDefault = !IsClickThrough
                    })
                .DistinctUntilChanged()
                .Select(
                    x => x.IsUnclocked || x.IsSelectedInMainWindow || x.IsNotClickThroughByDefault
                        ? OverlayMode.Layered
                        : OverlayMode.Transparent)
                .DistinctUntilChanged()
                .Subscribe(x => OverlayMode = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ThumbnailSize).ToUnit().Merge(this.WhenAnyValue(x => x.MaintainAspectRatio).ToUnit())
                .Where(x => thumbnailSize.IsNotEmpty())
                .Select(x => thumbnailSize)
                .Subscribe(HandleSourceSizeChange)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsInEditMode)
                .Subscribe(
                    x => thumbnailOpacity.SetValue(
                        x
                            ? EditModeThumbnailOpacity
                            : default(double?)))
                .AddTo(Anchors);
        }

        private void HandleSourceSizeChange(Size sourceSize)
        {
            if (!sourceSize.IsNotEmpty())
            {
                throw new ApplicationException($"SourceSize must be non-empty at this point, got {sourceSize} (maintainAspectRatio: {MaintainAspectRatio})");
            }

            double? aspectRatio = (double)sourceSize.Width / sourceSize.Height;
            if (maintainAspectRatio)
            {
                Log.Debug($"Handling Source size change: {sourceSize}, aspectRatio: {TargetAspectRatio} => {aspectRatio}");
                TargetAspectRatio = aspectRatio;
            }
            else
            {
                Log.Debug($"Handling Source size change: {sourceSize}, aspect ratio sync is disabled, source AspectRatio: {aspectRatio}");
                TargetAspectRatio = null;
            }
        }

        public override string ToString()
        {
            return $"[{OverlayName}]";
        }
    }
}