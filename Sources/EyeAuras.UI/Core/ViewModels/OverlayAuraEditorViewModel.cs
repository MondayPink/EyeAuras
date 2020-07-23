using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Dragablz;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Utilities;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraEditorViewModel : AuraPropertiesEditorBase<OverlayAuraModel>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraEditorViewModel));

        private readonly IAuraRepository repository;
        private readonly IFactory<IPropertyEditorViewModel> propertiesEditorFactory;
        private readonly IFactory<LinkedPositionMonitor<IPropertyEditorViewModel>> positionMonitorFactory;
        private readonly IScheduler uiScheduler;
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        private readonly DelegateCommand<object> addTriggerCommand;
        
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> triggerEditors;
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> onEnterActionEditors;
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> whileActiveActionEditors;
        private ReadOnlyObservableCollection<IPropertyEditorViewModel> onExitActionEditors;

        private PositionMonitor triggersPositionMonitor;
        private PositionMonitor onEnterActionsPositionMonitor;
        private PositionMonitor whileActiveActionsPositionMonitor;
        private PositionMonitor onExitActionsPositionMonitor;
        private IPropertyEditorViewModel coreEditor;
        private IAuraCore selectedCore;

        public OverlayAuraEditorViewModel(
            [NotNull] IAuraRepository repository,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] IFactory<LinkedPositionMonitor<IPropertyEditorViewModel>> positionMonitorFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.repository = repository;
            this.propertiesEditorFactory = propertiesEditorFactory;
            this.positionMonitorFactory = positionMonitorFactory;
            this.uiScheduler = uiScheduler;
            activeSourceAnchors.AddTo(Anchors);
            
            addTriggerCommand = new DelegateCommand<object>(AddTriggerCommandExecuted);
            AddOnEnterActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.OnEnterActions));
            AddOnExitActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.OnExitActions));
            AddWhileActiveActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.WhileActiveActions));
            CopyIdToClipboard = CommandWrapper.Create(() => clipboardManager.SetText(Source.Id), this.WhenAnyValue(x => x.Source).Select(x => x != null));

            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraTrigger)
                .Transform(x => x as IAuraTrigger)
                .Sort(new SortExpressionComparer<IAuraTrigger>().ThenByAscending(x => x.TriggerName))
                .Bind(out var knownTriggers)
                .Subscribe()
                .AddTo(Anchors);
            KnownTriggers = knownTriggers;
            
            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraAction)
                .Transform(x => x as IAuraAction)
                .Sort(new SortExpressionComparer<IAuraAction>().ThenByAscending(x => x.ActionName))
                .Bind(out var knownActions)
                .Subscribe()
                .AddTo(Anchors);
            KnownActions = knownActions;
            
            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraCore)
                .Transform(x => x as IAuraCore)
                .Bind(out var knownCores)
                .Subscribe()
                .AddTo(Anchors);
            KnownCores = knownCores;
             
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public int WhileActiveActionsTimeoutInMilliseconds
        {
            get => (int)Source.WhileActiveActionsTimeout.TotalMilliseconds;
            set => Source.WhileActiveActionsTimeout = TimeSpan.FromMilliseconds(value);
        }

        public IPropertyEditorViewModel CoreEditor
        {
            get => coreEditor;
            set => RaiseAndSetIfChanged(ref coreEditor, value);
        }

        public IAuraCore SelectedCore
        {
            get => selectedCore;
            set => RaiseAndSetIfChanged(ref selectedCore, value);
        }

        public ReadOnlyObservableCollection<IAuraTrigger> KnownTriggers { get; }
        
        public ReadOnlyObservableCollection<IAuraAction> KnownActions { get; }
        
        public ReadOnlyObservableCollection<IAuraCore> KnownCores { get; }

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> TriggerEditors
        {
            get => triggerEditors;
            private set => this.RaiseAndSetIfChanged(ref triggerEditors, value);
        }

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> OnEnterActionEditors
        {
            get => onEnterActionEditors;
            private set => this.RaiseAndSetIfChanged(ref onEnterActionEditors, value);
        }
        
        public ReadOnlyObservableCollection<IPropertyEditorViewModel> WhileActiveActionEditors
        {
            get => whileActiveActionEditors;
            set => this.RaiseAndSetIfChanged(ref whileActiveActionEditors, value);
        }

        public ReadOnlyObservableCollection<IPropertyEditorViewModel> OnExitActionEditors
        {
            get => onExitActionEditors;
            set => this.RaiseAndSetIfChanged(ref onExitActionEditors, value);
        }
        
        public ICommand AddTriggerCommand => addTriggerCommand;
        
        public CommandWrapper AddOnEnterActionCommand { get; }
        
        public CommandWrapper AddOnExitActionCommand { get; }
        
        public CommandWrapper AddWhileActiveActionCommand { get; }
        
        public CommandWrapper CopyIdToClipboard { get; }

        public PositionMonitor TriggersPositionMonitor
        {
            get => triggersPositionMonitor;
            private set => this.RaiseAndSetIfChanged(ref triggersPositionMonitor, value);
        }

        public PositionMonitor OnEnterActionsPositionMonitor
        {
            get => onEnterActionsPositionMonitor;
            private set => this.RaiseAndSetIfChanged(ref onEnterActionsPositionMonitor, value);
        }

        public PositionMonitor WhileActiveActionsPositionMonitor
        {
            get => whileActiveActionsPositionMonitor;
            private set => this.RaiseAndSetIfChanged(ref whileActiveActionsPositionMonitor, value);
        }

        public PositionMonitor OnExitActionsPositionMonitor
        {
            get => onExitActionsPositionMonitor;
            private set => this.RaiseAndSetIfChanged(ref onExitActionsPositionMonitor, value);
        }

        private void AddAction(object actionSample, IComplexAuraAction actions)
        {
            Guard.ArgumentNotNull(actionSample, nameof(actionSample));

            var model = repository.CreateModel<IAuraAction>(actionSample.GetType());
            actions.Insert(0, model);
        }

        private void AddTriggerCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var trigger = repository.CreateModel<IAuraTrigger>(obj.GetType());
            Source.Triggers.Insert(0, trigger);
        }
         
        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Disposable.Create(() =>
            {
                Source = null;
                OnEnterActionEditors = null;
                TriggerEditors = null;
            }).AddTo(sourceAnchors);

            CoreEditor = propertiesEditorFactory.Create();
            Source.WhenAnyValue(x => x.Core)
                .Subscribe(x => CoreEditor.Value = x)
                .AddTo(sourceAnchors);
            
            SubscribeAndCreate(Source.Triggers, out var triggersSource).AddTo(sourceAnchors);
            TriggerEditors = triggersSource;

            SubscribeAndCreate(Source.OnEnterActions, out var onEnterActionsSource).AddTo(sourceAnchors);
            OnEnterActionEditors = onEnterActionsSource;
            
            SubscribeAndCreate(Source.OnExitActions, out var onExitActionsSource).AddTo(sourceAnchors);
            OnExitActionEditors = onExitActionsSource;
            
            SubscribeAndCreate(Source.WhileActiveActions, out var whileActiveActionsSource).AddTo(sourceAnchors);
            WhileActiveActionEditors = whileActiveActionsSource;
            
            TriggersPositionMonitor = positionMonitorFactory.Create().AddTo(sourceAnchors).SyncWith(Source.Triggers, (x, y) => ReferenceEquals(x.Value, y));
            OnEnterActionsPositionMonitor = positionMonitorFactory.Create().AddTo(sourceAnchors).SyncWith(Source.OnEnterActions, (x, y) => ReferenceEquals(x.Value, y));
            WhileActiveActionsPositionMonitor = positionMonitorFactory.Create().AddTo(sourceAnchors).SyncWith(Source.WhileActiveActions, (x, y) => ReferenceEquals(x.Value, y));
            OnExitActionsPositionMonitor = positionMonitorFactory.Create().AddTo(sourceAnchors).SyncWith(Source.OnExitActions, (x, y) => ReferenceEquals(x.Value, y));

            Source.WhenAnyValue(x => x.Core)
                .Select(auraCore => KnownCores.FirstOrDefault(y => y.GetType() == auraCore?.GetType()))
                .Subscribe(auraCore => SelectedCore = auraCore, Log.HandleUiException)
                .AddTo(Anchors);
            this.WhenAnyValue(x => x.SelectedCore)
                .Where(x => x != null)
                .Where(x => Source?.Properties?.CoreProperties.GetType() != x.Properties?.GetType())
                .Subscribe(x =>
                {
                    //FIXME Rework Core creating method - current implementation re-initializes the WHOLE aura
                    var properties = Source.Properties;
                    properties.CoreProperties = x.Properties;
                    Source.Properties = properties;
                }, Log.HandleUiException)
                .AddTo(sourceAnchors);
        }

        private IDisposable SubscribeAndCreate<TAuraModel>(
            ISourceList<TAuraModel> collection, 
            out ReadOnlyObservableCollection<IPropertyEditorViewModel> editorCollection) where TAuraModel : IAuraModel
        {
            return collection
                .Connect()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<TAuraModel>(x, collection);
                        editor.SetCloseController(closeController);
                        editor.Value = x;
                        return editor;
                    })
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out editorCollection)
                .Subscribe();
        }
    }
}