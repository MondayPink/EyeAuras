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
using DynamicData.Binding;
using PoeShared;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class SharedContext : DisposableReactiveObject, IGlobalContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharedContext));

        public IComplexAuraTrigger SystemTrigger { get; }

        public ReadOnlyObservableCollection<IAuraViewModel> AuraList { get; }
        
        public ObservableCollection<IAuraTabViewModel> TabList { get; }

        public SharedContext()
        {
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
    }
}