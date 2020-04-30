using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EyeAuras.Shared;
using PoeShared;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public abstract partial class ScriptExecutorBase : DisposableReactiveObject, IScriptExecutor, IDictionary<string, object>
    {
        private readonly  CircularBuffer<string> output = new CircularBuffer<string>(1024);

        public IEnumerable<string> Output => output;

        private ISharedContext sharedContext;
        private IUnityContainer container;
        private IAuraContext auraContext;

        private readonly ConcurrentDictionary<IAuraProperties, IAuraModel> modelByProperties = new ConcurrentDictionary<IAuraProperties, IAuraModel>();

        public ISharedContext SharedContext
        {
            get => sharedContext ?? throw new InvalidOperationException($"{nameof(SharedContext)} is not initialized yet");
            private set => RaiseAndSetIfChanged(ref sharedContext, value);
        }

        public IAuraContext AuraContext
        {
            get => auraContext ?? throw new InvalidOperationException($"{nameof(AuraContext)} is not initialized yet");
            set => RaiseAndSetIfChanged(ref auraContext, value);
        }

        public IUnityContainer Container
        {
            get => container ?? throw new InvalidOperationException($"{nameof(Container)} is not initialized yet");
            private set => RaiseAndSetIfChanged(ref container, value);
        }

        public abstract void Execute();
        
        public void SetSharedContext(ISharedContext sharedContext)
        {
            SharedContext = sharedContext;
        }
        
        public void SetAuraContext(IAuraContext auraContext)
        {
            AuraContext = auraContext;
        }

        public void SetContainer(IUnityContainer container)
        {
            Container = container;
        }

        public IAuraViewModel FindAuraById(string auraId)
        {
            return SharedContext.AuraList.FirstOrDefault(x => x.Id == auraId);
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
        
        public void Log(object objectToDump)
        {
            Log(objectToDump.Dump());
        }

        public void Log(string message)
        {
            var now = Container.Resolve<IClock>().Now;
            var formattedMessage = $"{now:HH:mm:ss.fff} {message}";
            output.PushBack(formattedMessage);
            UpdateLog();
        }

        public void AddOrUpdate<T>(string key, T value)
        {
            AuraContext[key] = value;
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