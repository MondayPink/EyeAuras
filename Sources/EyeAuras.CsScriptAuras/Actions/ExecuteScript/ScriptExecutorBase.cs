using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EyeAuras.Shared;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public abstract class ScriptExecutorBase : DisposableReactiveObject, IScriptExecutor 
    {
        private readonly  CircularBuffer<string> output = new CircularBuffer<string>(1024);

        public IEnumerable<string> Output => output;

        private ISharedContext context;
        private IUnityContainer container;
        
        private readonly ConcurrentDictionary<IAuraProperties, IAuraModel> modelByProperties = new ConcurrentDictionary<IAuraProperties, IAuraModel>();

        public ISharedContext Context
        {
            get => context;
            private set => RaiseAndSetIfChanged(ref context, value);
        }

        public IUnityContainer Container
        {
            get => container;
            private set => RaiseAndSetIfChanged(ref container, value);
        }

        public abstract void Execute();
        
        public void SetContext(ISharedContext sharedContext)
        {
            Context = sharedContext;
        }

        public void SetContainer(IUnityContainer container)
        {
            Container = container;
        }

        public IAuraViewModel FindAuraById(string auraId)
        {
            return Context.AuraList.FirstOrDefault(x => x.Id == auraId);
        }

        public IAuraAction CreateAction(IAuraProperties auraProperties)
        {
            return GetOrCreate<IAuraAction>(auraProperties);
        }
        
        public IAuraTrigger CreateTrigger(IAuraProperties auraProperties)
        {
            return GetOrCreate<IAuraTrigger>(auraProperties);
        }

        public void LogClear()
        {
            while (!output.IsEmpty)
            {
                output.PopBack();
            }
        }

        public void Log(string message)
        {
            output.PushBack(message);
            UpdateLog();
        }

        private T GetOrCreate<T>(IAuraProperties auraProperties) where T : IAuraModel
        {
            if (!modelByProperties.TryGetValue(auraProperties, out var auraModel))
            {
                var repository = container.Resolve<IAuraRepository>();
                auraModel = modelByProperties[auraProperties] = repository.CreateModel<IAuraAction>(auraProperties).AddTo(Anchors);
            }
            return (T)auraModel;
        }
        
        private void UpdateLog()
        {
            this.RaisePropertyChanged(nameof(Output));
        }
    }
}