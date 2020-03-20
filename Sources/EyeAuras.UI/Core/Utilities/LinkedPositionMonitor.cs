using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Dragablz;
using DynamicData;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.Utilities
{
    internal sealed class LinkedPositionMonitor<T> : VerticalPositionMonitor, IDisposableReactiveObject
    {
        private readonly IScheduler bgScheduler;
        private readonly IScheduler uiScheduler;
        private static readonly ILog Log = LogManager.GetLogger(typeof(LinkedPositionMonitor<T>));

        public LinkedPositionMonitor(
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.bgScheduler = bgScheduler;
            this.uiScheduler = uiScheduler;
            Items = Array.Empty<T>();
            Observable
                .FromEventPattern<EventHandler<OrderChangedEventArgs>, OrderChangedEventArgs>(
                    h => OrderChanged += h,
                    h => OrderChanged -= h)
                .Subscribe(x => HandleOrderChanged(x.Sender, x.EventArgs))
                .AddTo(Anchors);
        }

        public LinkedPositionMonitor<T> SyncWith<TTarget>(
            ObservableCollection<TTarget> targetCollection,
            Func<T, TTarget, bool> comparer)
        {
            return SyncWith(() => AlignCollections(Items, targetCollection, ObservableCollectionMoveAdaptor, comparer));
        }

        public LinkedPositionMonitor<T> SyncWith<TTarget>(
            ISourceList<TTarget> targetCollection,
            Func<T, TTarget, bool> comparer)
        {
            return SyncWith(() => targetCollection.Edit(list => AlignCollections(Items, list, SourceListMoveAdaptor, comparer)));
        }
        
        public LinkedPositionMonitor<T> SyncWith(
            Action alignCollections)
        {
            this.WhenAnyValue(x => x.Items)
                .Throttle(TimeSpan.FromMilliseconds(500), bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(alignCollections)
                .AddTo(Anchors);
            return this;
        }

        public void Dispose()
        {
            Anchors.Dispose();
        }
        
        public CompositeDisposable Anchors { get; } = new CompositeDisposable();
        
        public T[] Items { get; private set; }
        
        private static void SourceListMoveAdaptor<TTarget>(IExtendedList<TTarget> list, int oldIdx, int newIdx)
        {
            list.Move(oldIdx, newIdx);
        }
        
        private static void ObservableCollectionMoveAdaptor<TTarget>(ObservableCollection<TTarget> list, int oldIdx, int newIdx)
        {
            list.Move(oldIdx, newIdx);
        }
        
        private static void AlignCollections<TTargetCollection, TTarget>(
            IList<T> orderedCollection,
            TTargetCollection targetCollection, 
            Action<TTargetCollection, int, int> moveAdaptor,
            Func<T, TTarget, bool> comparer) where TTargetCollection : IList<TTarget>
        {
            var changesLog = new List<string>();
            for (var newIndex = 0; newIndex < orderedCollection.Count; newIndex++)
            {
                var itemToMove = orderedCollection[newIndex];
                var oldIndex = IndexOf(targetCollection, target => comparer(itemToMove, target));
                
                if (oldIndex < 0 || oldIndex == newIndex)
                {
                    continue;
                }
                var oldItem = targetCollection[oldIndex];
                changesLog.Add($"Moving item {oldItem} from #{oldIndex} to {newIndex}");
                moveAdaptor(targetCollection, oldIndex, newIndex);
                newIndex = 0;
            }

            if (changesLog.Any())
            {
                Log.Debug($"Order changed:\n\t{changesLog.DumpToTable()}");
            }
        }
        
        private void HandleOrderChanged(object sender, OrderChangedEventArgs orderChangedEventArgs)
        {
            if (sender == null || orderChangedEventArgs == null)
            {
                return;
            }

            Log.Debug(
                $"Items order has changed, \nOld:\n\t{orderChangedEventArgs.PreviousOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}, \nNew:\n\t{orderChangedEventArgs.NewOrder.EmptyIfNull().Select(x => x?.ToString() ?? "(null)").DumpToTable()}");
            
            var orderedItems = orderChangedEventArgs.NewOrder
                .EmptyIfNull()
                .OfType<T>()
                .ToList();

            Items = orderedItems.ToArray();
            RaisePropertyChanged(nameof(Items));
        }

        private static int IndexOf<TItem>(IList<TItem> items, Predicate<TItem> condition)
        {
            for (var idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                if (condition(item))
                {
                    return idx;
                }
            }

            return -1;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}