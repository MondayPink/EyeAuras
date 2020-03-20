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
    public abstract class ComplexAuraAction<TAuraProperties> : AuraActionBase<TAuraProperties>, IComplexAuraAction where TAuraProperties : class, IAuraProperties, new()
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComplexAuraAction<TAuraProperties>));

        private readonly ISourceList<IAuraAction> actions = new SourceList<IAuraAction>();

        protected ComplexAuraAction()
        {
            Disposable.Create(() =>
            {
                Log.Debug($"Disposing ComplexAuraAction, items: {actions.Count}");
                actions.Items.ForEach(x => x.Dispose());
            }).AddTo(Anchors);
            
            actions
                .Connect()
                .DisposeMany()
                .Subscribe()
                .AddTo(Anchors);
        }

        public override void Execute()
        {
            actions.Items.ForEach(x => x.Execute());
        }

        public override string ActionName { get; } = "Multi-action";

        public override string ActionDescription { get; } =
            "Action which combines multiple child action into single one";
        
        public IObservable<IChangeSet<IAuraAction>> Connect(Func<IAuraAction, bool> predicate = null)
        {
            return actions.Connect(predicate);
        }

        public IObservable<IChangeSet<IAuraAction>> Preview(Func<IAuraAction, bool> predicate = null)
        {
            return actions.Preview(predicate);
        }

        public IObservable<int> CountChanged => actions.CountChanged;

        public IEnumerable<IAuraAction> Items => actions.Items;

        public int Count => actions.Count;
        
        public void Add(IAuraAction item)
        {
            Guard.ArgumentNotNull(item, nameof(item));
            actions.Add(item);
        }

        public bool Remove(IAuraAction item)
        {
            return actions.Remove(item);
        }

        public void Edit(Action<IExtendedList<IAuraAction>> updateAction)
        {
            actions.Edit(updateAction);
        }
    }
}