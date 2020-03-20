using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Scaffolding;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using log4net;
using PoeShared;

namespace EyeAuras.Shared
{
    public abstract class ComplexAuraTrigger<T> : AuraTriggerBase<T>, IComplexAuraTrigger where T : class, IAuraTriggerProperties, new()
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexAuraTrigger<T>));

        private readonly ISourceList<IAuraTrigger> triggers = new SourceList<IAuraTrigger>();
        
        public override string TriggerName { get; } = "Boolean AND Trigger";

        public override string TriggerDescription { get; } = "Trigger which combines multiple child triggers into single one using AND operation";
        
        protected ComplexAuraTrigger()
        {
            Disposable.Create(() =>
            {
                Log.Debug($"Disposing ComplexAuraTrigger, items: {triggers.Count}");
                triggers.Items.ForEach(x => x.Dispose());
            }).AddTo(Anchors);
            
            triggers
                .Connect()
                .DisposeMany()
                .Subscribe()
                .AddTo(Anchors);
            
            Observable.Merge(
                    triggers.Connect().WhenPropertyChanged(x => x.IsActive).ToUnit(),
                    triggers.Connect().ToUnit())
                .StartWithDefault()
                .Subscribe(() => IsActive = triggers.Items.All(x => x.IsActive))
                .AddTo(Anchors);
        }

        public IObservable<IChangeSet<IAuraTrigger>> Connect(Func<IAuraTrigger, bool> predicate = null)
        {
            return triggers.Connect(predicate);
        }

        public IObservable<IChangeSet<IAuraTrigger>> Preview(Func<IAuraTrigger, bool> predicate = null)
        {
            return triggers.Preview(predicate);
        }

        public IObservable<int> CountChanged => triggers.CountChanged;

        public IEnumerable<IAuraTrigger> Items => triggers.Items;

        public int Count => triggers.Count;
        
        public void Add(IAuraTrigger trigger)
        {
            Guard.ArgumentNotNull(trigger, nameof(trigger));
            triggers.Add(trigger);
        }

        public bool Remove(IAuraTrigger trigger)
        {
            Guard.ArgumentNotNull(trigger, nameof(trigger));
            return triggers.Remove(trigger);
        }

        public void Edit(Action<IExtendedList<IAuraTrigger>> updateAction)
        {
            triggers.Edit(updateAction);
        }
    }
}