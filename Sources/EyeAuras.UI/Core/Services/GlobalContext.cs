using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using EyeAuras.Shared;
using EyeAuras.UI.Core.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;
using System;
using System.Linq;
using DynamicData.Binding;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using PoeShared;
using PoeShared.UI;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class GlobalContext : DisposableReactiveObject, IGlobalContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GlobalContext));
        
        private readonly IUniqueIdGenerator idGenerator;
        private readonly IFactory<IOverlayAuraTabViewModel, OverlayAuraProperties> auraViewModelFactory;

        public GlobalContext(
            [NotNull] IUniqueIdGenerator idGenerator,
            [NotNull] IFactory<IOverlayAuraTabViewModel, OverlayAuraProperties> auraViewModelFactory)
        {
            this.idGenerator = idGenerator;
            this.auraViewModelFactory = auraViewModelFactory;
            SystemTrigger = new ComplexAuraTrigger().AddTo(Anchors);
            
            TabList = new ObservableCollection<IAuraTabViewModel>();
            
            TabList
                .ToObservableChangeSet()
                .DisposeMany()
                .Transform(x => (IAuraViewModel)x)
                .Bind(out var auraList)
                .Subscribe(() => { }, Log.HandleUiException)
                .AddTo(Anchors);
            AuraList = auraList;
        }
        
        public IComplexAuraTrigger SystemTrigger { get; }

        public ReadOnlyObservableCollection<IAuraViewModel> AuraList { get; }
        
        public ObservableCollection<IAuraTabViewModel> TabList { get; }

        public IAuraTabViewModel[] CreateAura(params OverlayAuraProperties[] properties)
        {
            return properties.Select(CreateAura).ToArray();
        }

        private IAuraTabViewModel CreateAura(OverlayAuraProperties tabProperties)
        {
            using var sw = new BenchmarkTimer("Create new tab", Log);

            Log.Debug($"Adding new tab {tabProperties.Name}");

            var existingTab = TabList.FirstOrDefault(x => x.Id == tabProperties.Id);
            if (existingTab != null)
            {
                var newId = idGenerator.Next();
                Log.Warn($"Tab with the same Id({tabProperties.Id}) already exists: {existingTab}, changing Id of a new tab to {newId}");
                tabProperties.Id = newId;
            }

            var auraViewModel = (IAuraTabViewModel)auraViewModelFactory.Create(tabProperties);
            sw.Step($"Created view model of type {auraViewModel.GetType()}: {auraViewModel}");
            sw.Step($"Initialized CloseController");

            TabList.Add(auraViewModel);
            sw.Step($"Added to AuraList(current count: {AuraList.Count})");

            return auraViewModel;
        }
    }
}