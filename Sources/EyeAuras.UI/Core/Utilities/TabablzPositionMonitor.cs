using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Dragablz;
using log4net;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.UI.Core.Utilities
{
    internal sealed class TabablzPositionMonitor<T> : VerticalPositionMonitor
    {
        private readonly IEnumerable<T> itemsSource;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TabablzPositionMonitor<T>));

        public TabablzPositionMonitor(ObservableCollection<T> itemsSource) : this()
        {
            this.itemsSource = itemsSource;
        }
        
        public CompositeDisposable Anchors { get; } = new CompositeDisposable();

        public TabablzPositionMonitor()
        {
            Items = Array.Empty<T>();
            OrderChanged += OnOrderChanged;
        }

        public T[] Items { get; private set; }

        private void OnOrderChanged(object sender, OrderChangedEventArgs orderChangedEventArgs)
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
            if (itemsSource != null)
            {
                orderedItems = itemsSource
                    .Select(x => new {Idx = orderedItems.IndexOf(x), Tab = x})
                    .OrderBy(x => x.Idx)
                    .Select(x => x.Tab)
                    .ToList();
            }

            Items = orderedItems.ToArray();
        }
    }
}