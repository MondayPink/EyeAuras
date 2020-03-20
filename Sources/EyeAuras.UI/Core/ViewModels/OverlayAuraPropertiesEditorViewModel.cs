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
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraPropertiesEditorViewModel : AuraPropertiesEditorBase<OverlayAuraModelBase>
    {
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
        
        public OverlayAuraPropertiesEditorViewModel(
            [NotNull] IAuraRepository repository,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IWindowSelectorViewModel windowSelector,
            [NotNull] IFactory<LinkedPositionMonitor<IPropertyEditorViewModel>> positionMonitorFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.repository = repository;
            this.propertiesEditorFactory = propertiesEditorFactory;
            this.positionMonitorFactory = positionMonitorFactory;
            this.uiScheduler = uiScheduler;
            WindowSelector = windowSelector.AddTo(Anchors);
            activeSourceAnchors.AddTo(Anchors);
            
            addTriggerCommand = new DelegateCommand<object>(AddTriggerCommandExecuted);
            AddOnEnterActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.OnEnterActions));
            AddOnExitActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.OnExitActions));
            AddWhileActiveActionCommand = CommandWrapper.Create<object>(x => AddAction(x, Source.WhileActiveActions));

            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraTrigger)
                .Transform(x => x as IAuraTrigger)
                .Bind(out var knownTriggers)
                .Subscribe()
                .AddTo(Anchors);
            KnownTriggers = knownTriggers;
            
            repository.KnownEntities
                .ToObservableChangeSet()
                .Filter(x => x is IAuraAction)
                .Transform(x => x as IAuraAction)
                .Bind(out var knownActions)
                .Subscribe()
                .AddTo(Anchors);
            KnownActions = knownActions;
             
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public int WhileActiveActionsTimeoutInMilliseconds
        {
            get => (int)Source.WhileActiveActionsTimeout.TotalMilliseconds;
            set => Source.WhileActiveActionsTimeout = TimeSpan.FromMilliseconds(value);
        }

        public IWindowSelectorViewModel WindowSelector { get; }
        
        public ReadOnlyObservableCollection<IAuraTrigger> KnownTriggers { get; }
        
        public ReadOnlyObservableCollection<IAuraAction> KnownActions { get; }

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
            actions.Add(model);
        }

        private void AddTriggerCommandExecuted(object obj)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            var trigger = repository.CreateModel<IAuraTrigger>(obj.GetType());
            Source.Triggers.Add(trigger);
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
            
            Source.WhenAnyValue(x => x.TargetWindow).Subscribe(x => WindowSelector.TargetWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.TargetWindow).Subscribe(x => Source.TargetWindow = x).AddTo(sourceAnchors);
            
            Source.Overlay.WhenAnyValue(x => x.AttachedWindow).Subscribe(x => WindowSelector.ActiveWindow = x).AddTo(sourceAnchors);
            WindowSelector.WhenAnyValue(x => x.ActiveWindow).Subscribe(x => Source.Overlay.AttachedWindow = x).AddTo(sourceAnchors);

            Source.Triggers
                .Connect()
                .Transform(
                    x =>
                    {
                        var editor = propertiesEditorFactory.Create();
                        var closeController = new RemoveItemController<IAuraTrigger>(x, Source.Triggers);
                        editor.SetCloseController(closeController);
                        editor.Value = x;
                        return editor;
                    })
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out var triggersSource)
                .Subscribe()
                .AddTo(sourceAnchors);

            TriggerEditors = triggersSource;

            SubscribeAndCreate(Source.OnEnterActions, out var onEnterActionsSource).AddTo(sourceAnchors);
            OnEnterActionEditors = onEnterActionsSource;
            
            SubscribeAndCreate(Source.OnExitActions, out var onExitActionsSource).AddTo(sourceAnchors);
            OnExitActionEditors = onExitActionsSource;
            
            SubscribeAndCreate(Source.WhileActiveActions, out var whileActiveActionsSource).AddTo(sourceAnchors);
            WhileActiveActionEditors = whileActiveActionsSource;
            
            TriggersPositionMonitor = positionMonitorFactory.Create().SyncWith(Source.Triggers, (x, y) => ReferenceEquals(x.Value, y));
            OnEnterActionsPositionMonitor = positionMonitorFactory.Create().SyncWith(Source.OnEnterActions, (x, y) => ReferenceEquals(x.Value, y));
            WhileActiveActionsPositionMonitor = positionMonitorFactory.Create().SyncWith(Source.WhileActiveActions, (x, y) => ReferenceEquals(x.Value, y));
            OnExitActionsPositionMonitor = positionMonitorFactory.Create().SyncWith(Source.OnExitActions, (x, y) => ReferenceEquals(x.Value, y));
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