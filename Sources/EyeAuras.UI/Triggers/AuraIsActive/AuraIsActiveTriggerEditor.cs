using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.Core.ViewModels;
using PoeShared.Scaffolding;
using ReactiveUI;    
using System;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTriggerEditor : AuraPropertiesEditorBase<AuraIsActiveTrigger>
    {
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();
        private IAuraViewModel aura;

        public AuraIsActiveTriggerEditor(ISharedContext sharedContext)
        {
            activeSourceAnchors.AddTo(Anchors);

            sharedContext.AuraList
                .ToObservableChangeSet()
                .Filter(this.WhenAnyValue(x => x.Source.Context.Id).Select(sourceAuraId => new Func<IAuraViewModel, bool>(aura => aura.Id != sourceAuraId)))
                .Bind(out var auraList)
                .Subscribe()
                .AddTo(Anchors);

            AuraList = auraList;
             
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<IAuraViewModel> AuraList { get; }

        public IAuraViewModel AuraTab
        {
            get => aura;
            set => this.RaiseAndSetIfChanged(ref aura, value);
        }

        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }
            
            Source.WhenAnyValue(x => x.AuraTab).Subscribe(aura => AuraTab = aura).AddTo(sourceAnchors);
            this.WhenAnyValue(x => x.AuraTab).Where(x => x != null).Subscribe(x => Source.AuraId = x?.Id).AddTo(sourceAnchors);
        }
    }
}