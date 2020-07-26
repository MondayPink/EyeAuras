using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using EyeAuras.Controls.ViewModels;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.MainWindow;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Overlay.Views;
using EyeAuras.UI.RegionSelector.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;
using Size = System.Windows.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal sealed class ReplicaOverlayViewModel : EyeOverlayViewModel, IReplicaOverlayViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReplicaOverlayViewModel));
        private readonly IOverlayWindowController overlayWindowController;
        private readonly IWindowListProvider windowListProvider;
        
        private readonly CommandWrapper resetRegionCommand;
        private readonly CommandWrapper selectRegionCommand;
        private readonly CommandWrapper setAttachedWindowCommand;
        private readonly CommandWrapper closeConfigEditorCommand;

        private readonly SerialDisposable activeConfigEditorAnchors = new SerialDisposable();
        private readonly Lazy<OverlayConfigEditor> configEditorSupplier;
        private IWindowHandle attachedWindow;

        public ReplicaOverlayViewModel(
            [NotNull] [Dependency(WellKnownWindows.AllWindows)] IWindowTracker mainWindowTracker,
            [NotNull] IOverlayWindowController overlayWindowController,
            [NotNull] IAuraModelController auraModelController,
            [NotNull] IWindowListProvider windowListProvider,
            [NotNull] ISelectionAdornerViewModel selectionAdorner,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler) : base(mainWindowTracker, auraModelController, uiScheduler)
        {
            using var sw = new BenchmarkTimer("Initialization", Log, nameof(ReplicaOverlayViewModel));
            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            this.overlayWindowController = overlayWindowController;
            this.windowListProvider = windowListProvider;
            activeConfigEditorAnchors.AddTo(Anchors);
            
            resetRegionCommand = CommandWrapper.Create(ResetRegionCommandExecuted, ResetRegionCommandCanExecute);
            selectRegionCommand = CommandWrapper.Create(SelectRegionCommandExecuted, SelectRegionCommandCanExecute);
            setAttachedWindowCommand = CommandWrapper.Create<IWindowHandle>(SetAttachedWindowCommandExecuted);
            closeConfigEditorCommand = CommandWrapper.Create(CloseConfigEditorCommandExecuted);

            ResetRegionCommandExecuted();
            
            configEditorSupplier = new Lazy<OverlayConfigEditor>(() => CreateConfigEditor(this));
            sw.Step("Initialized Config editor");

            WhenLoaded
                .Take(1)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);
            
            this.WhenAnyProperty(x => x.AttachedWindow)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsInitialized)))
                .AddTo(Anchors);
        }

        private void ApplyConfig(Unit obj)
        {
            this.WhenAnyValue(x => x.AttachedWindow)
                .Where(x => x != null)
                .Subscribe(HandleInitialWindowAttachment)
                .AddTo(Anchors);
        }

        public ISelectionAdornerViewModel SelectionAdorner { get; }
        
        public ReadOnlyObservableCollection<IWindowHandle> WindowList => windowListProvider.WindowList;
        
        public ICommand ResetRegionCommand => resetRegionCommand;
        
        public ICommand SelectRegionCommand => selectRegionCommand;

        public ICommand SetAttachedWindowCommand => setAttachedWindowCommand;
        
        public ICommand CloseConfigEditorCommand => closeConfigEditorCommand;

        public IWindowHandle AttachedWindow
        {
            get => attachedWindow;
            set => RaiseAndSetIfChanged(ref attachedWindow, value);
        }

        private bool ResetRegionCommandCanExecute()
        {
            return AttachedWindow != null;
        }

        private bool SelectRegionCommandCanExecute()
        {
            return AttachedWindow != null && !IsInSelectMode;
        }

        private void HandleInitialWindowAttachment()
        {
            Title = $"Overlay {AttachedWindow.Title}";
        }

        protected override void ApplyConfig(IOverlayConfig config)
        {
            base.ApplyConfig(config);
                        
            this.WhenAnyValue(x => x.AttachedWindow)
                .Subscribe(() => resetRegionCommand.RaiseCanExecuteChanged())
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsInSelectMode, x => x.AttachedWindow)
                .Subscribe(() => selectRegionCommand.RaiseCanExecuteChanged())
                .AddTo(Anchors);
        }

        private void SelectRegionCommandExecuted()
        {
            using var unused = new OperationTimer(elapsed => Log.Debug($"SelectRegion initialization took {elapsed.TotalMilliseconds:F0}ms"));
            Log.Debug($"Region selection mode turned on, Region: {Region}");
            var selectRegionAnchors = OpenConfigEditor().AssignTo(activeConfigEditorAnchors);
            Disposable.Create(() =>
            {
                Log.Debug("Disabling Region selection");
                IsInSelectMode = false;
            }).AddTo(selectRegionAnchors);
            
            IsInSelectMode = true;
            SelectionAdorner.StartSelection()
                .Take(1)
                .Where(x => x.Width * x.Height >= 20)
                .Finally(() => selectRegionAnchors.Dispose())
                .Subscribe(UpdateRegion)
                .AddTo(selectRegionAnchors);
        }
        
        private void SetAttachedWindowCommandExecuted(IWindowHandle obj)
        {
            AttachedWindow = obj;
        }
        
        private CompositeDisposable OpenConfigEditor()
        {
            var anchors = new CompositeDisposable();

            configEditorSupplier.Value.Left = OverlayWindow.Left + OverlayWindow.ActualWidth + 10;
            configEditorSupplier.Value.Top = OverlayWindow.Top;
            
            Disposable.Create(
                    () =>
                    {
                        Log.Debug("Hiding ConfigEditor window");
                        configEditorSupplier.Value.Hide();
                        CloseConfigEditorCommandExecuted();
                    })
                .AddTo(anchors);
            
            overlayWindowController
                .WhenAnyValue(x => x.IsVisible)
                .Subscribe(
                    x =>
                    {
                        if (x)
                        {
                            configEditorSupplier.Value.Show();
                        }
                        else
                        {
                            configEditorSupplier.Value.Hide();
                        }
                    })
                .AddTo(anchors);

            return anchors;
        }

        private OverlayConfigEditor CreateConfigEditor(object dataContext)
        {
            using var unused = new OperationTimer(elapsed => Log.Debug($"ConfigEditor initialization took {elapsed.TotalMilliseconds:F0}ms"));

            var window = new OverlayConfigEditor
            {
                DataContext = dataContext,
                ShowActivated = false,
                Visibility = Visibility.Collapsed
            };
            
            Disposable.Create(
                    () =>
                    {
                        Log.Debug("Closing ConfigEditor window");
                        window.Close();
                    })
                .AddTo(Anchors);
            
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.Closed += h, h => window.Closed -= h)
                .Subscribe(CloseConfigEditorCommandExecuted)
                .AddTo(Anchors);
            
            return window;
        }

        private void ResetRegionCommandExecuted()
        {
            IsInSelectMode = false;
            Region.SetValue(Rectangle.Empty);
        }
        
        private void CloseConfigEditorCommandExecuted()
        {
            Log.Debug("Closing ConfigEditor");
            activeConfigEditorAnchors.Disposable = null;
        }
        
        private void UpdateRegion(Rect selection)
        {
            if (selection.IsEmpty)
            {
                Region.SetValue(WinRectangle.Empty);
                return;
            }
            selection.Scale(Dpi.DpiScaleX, Dpi.DpiScaleY); // Wpf Px => Win Px
            
            var targetSize = SourceWindowSize; // Win Px
            var destinationSize = new Size(ActualWidth, ActualHeight).Scale(Dpi.DpiScaleX, Dpi.DpiScaleY).ToWinSize(); // Win Px
            var currentTargetRegion = Region.Bounds; // Win Px

            var selectionPercent = new Rect
            {
                X = selection.X / destinationSize.Width,
                Y = selection.Y / destinationSize.Height,
                Height = selection.Height / destinationSize.Height,
                Width = selection.Width / destinationSize.Width
            };

            Rect currentRegionPercent;
            if (currentTargetRegion.IsNotEmpty())
            {
                currentRegionPercent = new Rect
                {
                    X = (double)currentTargetRegion.X / targetSize.Width,
                    Y = (double)currentTargetRegion.Y / targetSize.Height,
                    Height = (double)currentTargetRegion.Height / targetSize.Height,
                    Width = (double)currentTargetRegion.Width / targetSize.Width
                };
            }
            else
            {
                currentRegionPercent = new Rect
                {
                    Width = 1,
                    Height = 1
                };
            }

            var destinationRegion = new Rect
            {
                X = (currentRegionPercent.X + selectionPercent.X * currentRegionPercent.Width) * targetSize.Width,
                Y = (currentRegionPercent.Y + selectionPercent.Y * currentRegionPercent.Height) * targetSize.Height,
                Width = Math.Max(1, currentRegionPercent.Width * selectionPercent.Width * targetSize.Width),
                Height = Math.Max(1, currentRegionPercent.Height * selectionPercent.Height * targetSize.Height)
            };

            Region.SetValue(destinationRegion.ToWinRectangle());
        }

        public override bool IsInitialized => AttachedWindow != null;
    }
}