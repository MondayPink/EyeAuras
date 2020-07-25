using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.OnTopReplica;
using EyeAuras.OnTopReplica.WindowSeekers;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;
using ObservableEx = PoeShared.Scaffolding.ObservableEx;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class WindowListProvider : DisposableReactiveObject, IWindowListProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowListProvider));
        private static readonly IEqualityComparer<IWindowHandle> WindowEqualityComparer = new LambdaComparer<IWindowHandle>((x, y) => x?.Handle == y?.Handle && string.Compare(x?.Title, y?.Title, StringComparison.Ordinal) == 0);
        private static readonly IComparer<IWindowHandle> WindowComparer = new SortExpressionComparer<IWindowHandle>().ThenByAscending(x => x.Title).ThenByAscending(x => x.Title?.Length ?? int.MaxValue);
        
        private readonly ReadOnlyObservableCollection<IWindowHandle> windowList;
        private readonly SourceList<IWindowHandle> windowListSource = new SourceList<IWindowHandle>();

        private readonly IWindowSeeker windowSeeker;

        public WindowListProvider([NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            windowListSource
                .Connect()
                .Filter(x => !string.IsNullOrWhiteSpace(x.Title))
                .Sort(WindowComparer)
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out windowList)
                .Subscribe()
                .AddTo(Anchors);

            windowSeeker = new TaskWindowSeeker
            {
                SkipNotVisibleWindows = true
            };

            ObservableEx.BlockingTimer(TimeSpan.FromSeconds(1))
                .Subscribe(RefreshWindowList)
                .AddTo(Anchors);
        }

        public IWindowHandle ResolveByHandle(IntPtr hWnd)
        {
            return windowListSource.Items.FirstOrDefault(x => x.Handle == hWnd);
        }

        public ReadOnlyObservableCollection<IWindowHandle> WindowList => windowList;
        
        private void RefreshWindowList()
        {
            using var profiler = new BenchmarkTimer("Window list refresh", Log);
            windowSeeker.Refresh();
            profiler.Step("API call");
            
            var existingWindows = windowListSource.Items.ToArray();
            var itemsToAdd = windowSeeker.Windows.Except(existingWindows, WindowEqualityComparer).ToArray();
            var itemsToRemove = existingWindows.Except(windowSeeker.Windows, WindowEqualityComparer).ToArray();
            
            windowListSource.RemoveMany(itemsToRemove);
            windowListSource.AddRange(itemsToAdd);
            profiler.Step("Items add/remove");
        }
    }
}