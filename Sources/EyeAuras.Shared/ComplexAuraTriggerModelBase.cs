using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Scaffolding;
using System;
using System.Reactive.Disposables;
using log4net;

namespace EyeAuras.Shared
{
    public abstract class ComplexAuraTrigger<T> : AuraTriggerBase<T>, IComplexAuraTrigger where T : class, IAuraProperties
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexAuraTrigger));

        private readonly ISourceList<IAuraTrigger> triggers = new SourceList<IAuraTrigger>();
        
        public override string TriggerName { get; } = "Boolean AND Trigger";

        public override string TriggerDescription { get; } = "Trigger which combines multiple child triggers into single one using AND operation";
        
        public ObservableCollection<IAuraTrigger> Triggers { get; }

        protected ComplexAuraTrigger()
        {
            Disposable.Create(() =>
            {
                Log.Debug($"Disposing ComplexAura, items: {Triggers.Count}");
                Triggers.ForEach(x => x.Dispose());
            }).AddTo(Anchors);
            
            var triggerList = new ObservableCollectionExtended<IAuraTrigger>();
            triggers
                .Connect()
                .DisposeMany()
                .Bind(triggerList)
                .Subscribe()
                .AddTo(Anchors);
            Triggers = triggerList;
            
            Observable.Merge(
                    Triggers.ToObservableChangeSet().WhenPropertyChanged(x => x.IsActive).ToUnit(),
                    Triggers.ToObservableChangeSet().ToUnit())
                .StartWithDefault()
                .Subscribe(() => IsActive = Triggers.All(x => x.IsActive))
                .AddTo(Anchors);
        }
    }
}