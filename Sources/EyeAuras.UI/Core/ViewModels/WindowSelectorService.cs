using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class WindowSelectorService : DisposableReactiveObject, IWindowSelectorService
    {
        private readonly IWindowMatcher windowMatcher;
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowSelectorService));

        private readonly ObservableAsPropertyHelper<bool> enableOverlaySelector;

        private WindowHandle activeWindow;
        private WindowHandle[] matchingWindowList = Array.Empty<WindowHandle>();
        private WindowMatchParams targetWindow;
        private string windowTitle;
        private bool windowTitleIsRegex;
        private IntPtr windowHandle;

        public WindowSelectorService(
            [NotNull] IWindowMatcher windowMatcher,
            [NotNull] IWindowListProvider windowListProvider)
        {
            this.windowMatcher = windowMatcher;
            WindowList = windowListProvider.WindowList;

            this.WhenAnyValue(x => x.WindowTitle)
                .WithPrevious((prev, curr) => new {prev, curr})
                .DistinctUntilChanged()
                .Where(x => !string.IsNullOrEmpty(x.prev) && x.curr == null)
                .Subscribe(x => WindowTitle = x.prev)
                .AddTo(Anchors);
        
            windowListProvider.WindowList.ToObservableChangeSet()
                .ToUnit()
                .Merge(this.WhenAnyValue(x => x.TargetWindow).ToUnit())
                .Subscribe(x => MatchingWindowList = BuildMatches(windowListProvider.WindowList))
                .AddTo(Anchors);
                
            this.WhenAnyValue(x => x.ActiveWindow)
                .Where(x => x != null)
                .Where(x => !windowMatcher.IsMatch(x, TargetWindow))
                .Subscribe(
                    x =>
                    {
                        var newTargetWindow = new WindowMatchParams
                        {
                            Title = x.Title,
                            Handle = x.Handle
                        };
                        Log.Debug($"Selected non-matching Overlay source, changing TargetWindow, {TargetWindow} => {newTargetWindow}");
                        TargetWindow = newTargetWindow;
                    })
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.MatchingWindowList)
                .Where(items => !items.Contains(ActiveWindow))
                .Select(items => items.FirstOrDefault(x => x.Handle == ActiveWindow?.Handle || x.Handle == WindowHandle) ?? items.FirstOrDefault())
                .Where(x => !Equals(ActiveWindow, x))
                .Subscribe(
                    x =>
                    {
                        Log.Debug(
                            $"Setting new Overlay Window(target: {TargetWindow}): {(ActiveWindow == null ? "null" : ActiveWindow.ToString())} => {(x == null ? "null" : x.ToString())}\n\t{MatchingWindowList.DumpToTable()}");
                        ActiveWindow = x;
                    })
                .AddTo(Anchors);
            
            enableOverlaySelector = this.WhenAnyProperty(x => x.MatchingWindowList)
                .Select(change => MatchingWindowList.Length > 1)
                .ToPropertyHelper(this, x => x.EnableOverlaySelector)
                .AddTo(Anchors);
                
            this.WhenAnyValue(x => x.TargetWindow)
                .Subscribe(
                    x =>
                    {
                        WindowTitle = x.Title;
                        WindowTitleIsRegex = x.IsRegex;
                        WindowHandle = x.Handle;
                    })
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.WindowTitle, x => x.WindowTitleIsRegex, x => x.WindowHandle)
                .Select(
                    x => new WindowMatchParams
                    {
                        Title = WindowTitle,
                        IsRegex = WindowTitleIsRegex,
                        Handle = WindowHandle
                    })
                .DistinctUntilChanged()
                .Subscribe(x => TargetWindow = x)
                .AddTo(Anchors);
            
            SetWindowTitleCommand = CommandWrapper.Create<WindowHandle>(SetWindowTitleCommandExecuted);
        }

        public string WindowTitle
        {
            get => windowTitle;
            set => RaiseAndSetIfChanged(ref windowTitle, value);
        }

        public bool WindowTitleIsRegex
        {
            get => windowTitleIsRegex;
            set => RaiseAndSetIfChanged(ref windowTitleIsRegex, value);
        }

        public IntPtr WindowHandle
        {
            get => windowHandle;
            set => this.RaiseAndSetIfChanged(ref windowHandle, value);
        }

        public ReadOnlyObservableCollection<WindowHandle> WindowList { get; }

        public ICommand SetWindowTitleCommand { get; }

        public bool EnableOverlaySelector => enableOverlaySelector.Value;

        public WindowHandle ActiveWindow
        {
            get => activeWindow;
            set => RaiseAndSetIfChanged(ref activeWindow, value);
        }

        public WindowHandle[] MatchingWindowList
        {
            get => matchingWindowList;
            private set => RaiseAndSetIfChanged(ref matchingWindowList, value);
        }

        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }
        
        private void SetWindowTitleCommandExecuted(WindowHandle handle)
        {
            WindowTitle = handle.Title;
        }

        private WindowHandle[] BuildMatches(IEnumerable<WindowHandle> source)
        {
            var comparer = new SortExpressionComparer<WindowHandle>()
                .ThenByAscending(x => x.Title.Length)
                .ThenByAscending(x => x.Title);
            var windowList = source
                .Where(x => windowMatcher.IsMatch(x, targetWindow))
                .OrderBy(x => x, comparer)
                .ToArray();
            return windowList;
        }
    }
}