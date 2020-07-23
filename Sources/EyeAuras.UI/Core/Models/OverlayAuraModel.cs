using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayAuraModel : AuraModelBase<OverlayAuraProperties>, IOverlayAuraModel, IAuraContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraModel));
        private static readonly TimeSpan ModelsReloadTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan TriggerDefaultThrottle = TimeSpan.FromMilliseconds(10);
        private static int GlobalAuraIdx;
        private static readonly TimeSpan PropertiesSamplingTimeout = TimeSpan.FromSeconds(5);

        private readonly IGlobalContext globalContext;
        private readonly IAuraRepository repository;
        private readonly IConfigSerializer configSerializer;

        private readonly SerialDisposable activeCoreAnchors;
        private readonly ComplexAuraAction onEnterActionsHolder = new ComplexAuraAction();
        private readonly ComplexAuraAction whileActiveActionsHolder = new ComplexAuraAction();
        private readonly ComplexAuraAction onExitActionsHolder = new ComplexAuraAction();
        private readonly ComplexAuraTrigger triggersHolder = new ComplexAuraTrigger();
        private readonly SourceCache<KeyValuePair<string, object>, string> variables = new SourceCache<KeyValuePair<string, object>, string>(x => x.Key);

        private ICloseController closeController;
        private bool isActive;
        private bool isEnabled = true;
        private string name;
        private string uniqueId;
        private TimeSpan whileActiveActionsTimeout;
        private string path;
        private IAuraCore core;

        public OverlayAuraModel(
            [NotNull] IGlobalContext globalContext,
            [NotNull] IAuraRepository repository,
            [NotNull] IConfigSerializer configSerializer,
            [NotNull] IUniqueIdGenerator idGenerator,
            [NotNull] ISchedulerProvider schedulerProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            activeCoreAnchors = new SerialDisposable().AddTo(Anchors);
            var defaultAuraName = $"Aura #{Interlocked.Increment(ref GlobalAuraIdx)}";
            Name = defaultAuraName;
            Id = idGenerator.Next();
            using var sw = new BenchmarkTimer($"[{this}] OverlayAuraModel initialization", Log, nameof(OverlayAuraModel));
            var bgScheduler = schedulerProvider.GetOrCreate($"{defaultAuraName}");
            Triggers = triggersHolder;
            OnEnterActions = onEnterActionsHolder;
            OnExitActions = onExitActionsHolder;
            WhileActiveActions = whileActiveActionsHolder;
            Variables = variables;
            Context = this;

            Triggers.Connect().OnItemAdded(x => x.Context = this).Subscribe().AddTo(Anchors);
            OnEnterActions.Connect().OnItemAdded(x => x.Context = this).Subscribe().AddTo(Anchors);
            OnExitActions.Connect().OnItemAdded(x => x.Context = this).Subscribe().AddTo(Anchors);
            WhileActiveActions.Connect().OnItemAdded(x => x.Context = this).Subscribe().AddTo(Anchors);

            this.globalContext = globalContext;
            this.repository = repository;
            this.configSerializer = configSerializer;

            var triggerIsActive = Observable.CombineLatest(
                    triggersHolder.WhenAnyValue(x => x.IsActive),
                    globalContext.SystemTrigger.WhenAnyValue(x => x.IsActive))
                .Select(x => (Triggers.Count > 0 && Triggers.IsActive || Triggers.Count == 0) && globalContext.SystemTrigger.IsActive)
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new
                {
                    prev, 
                    curr, 
                    triggers = $"{Triggers.Items.Select(x => x.ToString()).DumpToTextRaw()}", 
                    systemTriggers = $"{globalContext.SystemTrigger.Items.Select(x => x.ToString()).DumpToTextRaw()}"
                })
                .Throttle(TriggerDefaultThrottle, bgScheduler);
            
            //FIXME Move Trigger processing to BG scheduler
            triggerIsActive
                .ObserveOn(uiScheduler)
                .Subscribe(x =>
                {
                    Log.Debug($"[{this}] Updating IsActive {IsActive} => {x.curr}, change: {x}");
                    IsActive = x.curr;
                })
                .AddTo(Anchors);

            var triggerActivates =
                triggerIsActive
                    .Where(x => x.prev == false && x.curr)
                    .Publish();
            
            var triggerDeactivates = 
                triggerIsActive
                    .Where(x => x.prev == true && !x.curr)
                    .Publish();
            
            triggerActivates
                .Where(x => OnEnterActions.Count > 0)
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteOnEnterActions, Log.HandleUiException)
                .AddTo(Anchors);
            triggerDeactivates
                .Where(x => OnExitActions.Count > 0)
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteOnExitActions, Log.HandleUiException)
                .AddTo(Anchors);
            
            //FIXME Fix WhileActive mess - remove timer, make it event-driven
            triggerIsActive
                .Select(
                    x =>
                    {
                        Log.Debug($"[{this}] IsActive changed, IsActive: {x}");
                        return x.curr
                            ? Observable.Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(50), bgScheduler).ToUnit()
                            : Observable.Empty<Unit>();
                    })
                .Switch()
                .Where(x => WhileActiveActions.Count > 0)
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteWhileActiveActions, Log.HandleUiException)
                .AddTo(Anchors);
            triggerActivates.Connect().AddTo(Anchors);
            triggerDeactivates.Connect().AddTo(Anchors);
            
            this.repository.KnownEntities
                .ToObservableChangeSet()
                .SkipInitial()
                .Throttle(ModelsReloadTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    () =>
                    {
                        var properties = Properties;
                        ReloadCollections(properties);
                    })
                .AddTo(Anchors);

            Observable.Merge(
                    this.WhenAnyValue(x => x.Core).Select(x => x == null ? Observable.Empty<Unit>() : x.WhenAnyProperty(y => y.Properties).ToUnit()).Switch().Select(x => $"[{Name}].{Core} properties changed"),
                    OnEnterActions.Connect().Select(x => $"[{Name}({Id})] [OnEnter]  Action list changed, item count: {OnEnterActions.Count}"),
                    OnExitActions.Connect().Select(x => $"[{Name}({Id})] [OnExit]  Action list changed, item count: {OnEnterActions.Count}"),
                    WhileActiveActions.Connect().Select(x => $"[{Name}({Id})] [WhileActive] Action list changed, item count: {OnEnterActions.Count}"),
                    Triggers.Connect().Select(x => $"[{Name}({Id})] Trigger list changed, item count: {Triggers.Count}"),
                    Triggers.Connect().WhenPropertyChanged(x => x.Properties).Select(x => $"[{Name}] [{x.Sender}] Trigger Properties changed"),
                    OnEnterActions.Connect().WhenPropertyChanged(x => x.Properties).Select(x => $"[{Name}] [{x.Sender}] [OnEnter] Action Properties changed"),
                    OnExitActions.Connect().WhenPropertyChanged(x => x.Properties).Select(x => $"[{Name}] [{x.Sender}] [OnExit] Action Properties changed"),
                    WhileActiveActions.Connect().WhenPropertyChanged(x => x.Properties).Select(x => $"[{Name}] [{x.Sender}] [WhileActive] Action Properties changed"))
                .Buffer(PropertiesSamplingTimeout, bgScheduler)
                .Where(x => x.Count > 0)
                .Do(
                    reasons =>
                    {
                        const int maxReasonsToOutput = 50;
                        Log.Debug($"[{this}] Properties have changed({(reasons.Count <= maxReasonsToOutput ? $"{reasons.Count} items" : $"first {maxReasonsToOutput} of {reasons.Count} items")}):\r\n\t{reasons.Take(maxReasonsToOutput).JoinStrings("\r\n\t")}");
                    },
                    Log.HandleUiException)
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
            sw.Step($"Overlay model properties initialized");

            triggersHolder.AddTo(Anchors);
            whileActiveActionsHolder.AddTo(Anchors);
            onEnterActionsHolder.AddTo(Anchors);
            onExitActionsHolder.AddTo(Anchors);
            Disposable.Create(() =>
            {
                Log.Debug(
                    $"[{this}] Disposed Aura {Name}({Id}) (aka {defaultAuraName}), triggers: {Triggers.Count}");
            }).AddTo(Anchors);
        }

        private void ExecuteOnEnterActions()
        {
            Log.Debug($"[{this}] Executing OnEnter actions: {OnEnterActions.Items.Select(x => x.ToString()).DumpToTextRaw()}, triggers: {Triggers.Items.Select(x => x.ToString()).DumpToTextRaw()}");
            OnEnterActions.Items.ForEach(x => x.Execute());
        }
        
        private void ExecuteOnExitActions()
        {
            Log.Debug($"[{this}] Executing OnExit actions: {OnExitActions.Items.Select(x => x.ToString()).DumpToTextRaw()}, triggers: {Triggers.Items.Select(x => x.ToString()).DumpToTextRaw()}");
            OnExitActions.Items.ForEach(x => x.Execute());
        }
        
        private void ExecuteWhileActiveActions()
        {
            Log.Debug($"[{this}] Executing WhileActive actions: {WhileActiveActions.Items.Select(x => x.ToString()).DumpToTextRaw()}, triggers: {Triggers.Items.Select(x => x.ToString()).DumpToTextRaw()}");
            foreach (var action in WhileActiveActions.Items)
            {
                if (!IsActive)
                {
                    break;
                }
                action.Execute();
            }
            if (IsActive)
            {
                Thread.Sleep(WhileActiveActionsTimeout);
            }  
        }
        
        public bool IsActive
        {
            get => isActive;
            private set => RaiseAndSetIfChanged(ref isActive, value);
        }

        public IAuraCore Core
        {
            get => core;
            private set => RaiseAndSetIfChanged(ref core, value);
        }

        [AuraProperty]
        public bool IsEnabled
        {
            get => isEnabled;
            set => RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public string Id
        {
            get => uniqueId;
            private set => this.RaiseAndSetIfChanged(ref uniqueId, value);
        }

        public IComplexAuraTrigger Triggers { get; }

        public IComplexAuraAction OnEnterActions { get; }
        
        public IComplexAuraAction WhileActiveActions { get; }
        
        public IComplexAuraAction OnExitActions { get; }
        
        public SourceCache<KeyValuePair<string, object>, string> Variables { get; }

        public object this[string key]
        {
            get
            {
                var result = Variables.Lookup(key);
                return result.HasValue ? result.Value.Value : default;
            }
            set => Variables.Edit(x => x.AddOrUpdate(new KeyValuePair<string, object>(key, value)));
        }

        [AuraProperty]
        public TimeSpan WhileActiveActionsTimeout
        {
            get => whileActiveActionsTimeout;
            set => this.RaiseAndSetIfChanged(ref whileActiveActionsTimeout, value);
        }

        public void SetCloseController(ICloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        public ICloseController CloseController
        {
            get => closeController;
            private set => RaiseAndSetIfChanged(ref closeController, value);
        }

        [AuraProperty]
        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }

        [AuraProperty]
        public string Path
        {
            get => path;
            set => RaiseAndSetIfChanged(ref path, value);
        }

        private void ReloadCollections(OverlayAuraProperties source)
        {
            WhileActiveActions.Edit(x => x.Clear());;
            OnExitActions.Edit(x => x.Clear());;
            OnEnterActions.Edit(x => x.Clear());;
            Triggers.Edit(x => x.Clear());;
            
            source.TriggerProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(CreateModel<IAuraTrigger>)
                .ForEach(x => Triggers.Add(x));
            
            source.OnEnterActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(CreateModel<IAuraAction>)
                .ForEach(x => OnEnterActions.Add(x));
            
            source.OnExitActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(CreateModel<IAuraAction>)
                .ForEach(x => OnExitActions.Add(x));
            
            source.WhileActiveActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(CreateModel<IAuraAction>)
                .ForEach(x => WhileActiveActions.Add(x));
        }

        private T CreateModel<T>(IAuraProperties properties) where T : IAuraModel
        {
            var result = repository.CreateModel<T>(properties);
            return result;
        }

        protected override void VisitLoad(OverlayAuraProperties source)
        {
            if (!string.IsNullOrEmpty(source.Id))
            {
                Id = source.Id;
            }
            if (!string.IsNullOrEmpty(source.Name))
            {
                Name = source.Name;
            }
            Path = source.Path;

            WhileActiveActionsTimeout = source.WhileActiveActionsTimeout;
            IsEnabled = source.IsEnabled;
            ReloadCollections(source);

            if (source.CoreProperties != null)
            {
                //FIXME If same type of core is recreated twice, it disappears
                Core = CreateModel<IAuraCore>(source.CoreProperties);
                activeCoreAnchors.Disposable = Core;
                Core.ModelController = this;
                Core.Context = this;
                Core.Properties = source.CoreProperties;
            }
        }

        protected override void VisitSave(OverlayAuraProperties properties)
        {
            properties.Name = Name;
            properties.TriggerProperties = Triggers.Items.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.OnEnterActionProperties = OnEnterActions.Items.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.WhileActiveActionProperties = WhileActiveActions.Items.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.OnExitActionProperties = OnExitActions.Items.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.WhileActiveActionsTimeout = WhileActiveActionsTimeout;

            properties.IsEnabled = IsEnabled;
            properties.Id = Id;
            properties.Path = Path;
            properties.CoreProperties = Core?.Properties;
        }

        private IAuraProperties ToAuraProperties(PoeConfigMetadata<IAuraProperties> metadata)
        {
            if (metadata.Value == null)
            {
                Log.Debug($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
                var serialized = configSerializer.Serialize(metadata);
                if (string.IsNullOrEmpty(serialized))
                {
                    throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
                }
                var deserialized = configSerializer.Deserialize<PoeConfigMetadata<IAuraProperties>>(serialized);
                if (deserialized.Value != null)
                {
                    Log.Debug($"Successfully restored type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}: {deserialized.Value}");
                    metadata = deserialized;
                }
                else
                {
                    Log.Warn($"Failed to restore type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}");
                }
            }
            
            if (metadata.Value == null)
            {
                return new ProxyAuraProperties(metadata);
            }

            return metadata.Value;
        }
        
        private PoeConfigMetadata<IAuraProperties> ToMetadata(IAuraProperties properties)
        {
            if (properties is ProxyAuraProperties proxyAuraProperties)
            {
                return proxyAuraProperties.Metadata;
            }
            
            return new PoeConfigMetadata<IAuraProperties>
            {
                AssemblyName = properties.GetType().Assembly.GetName().Name,
                TypeName = properties.GetType().FullName,
                Value = properties,
                Version = properties.Version,
            };
        }
        
        private bool ValidateProperty(IAuraProperties properties)
        {
            if (properties == null)
            {
                return false;
            }
            
            if (properties is EmptyAuraProperties)
            {
                Log.Warn($"[{this}] {nameof(EmptyAuraProperties)} should never be used for Models Save/Load purposes - too generic");
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return $"{Name}({Id}){(IsActive ? " Active" : string.Empty)}";
        }
    }
}