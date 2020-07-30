using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.MainWindow.Services;
using JetBrains.Annotations;
using log4net;
using Microsoft.Win32.TaskScheduler;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Task = System.Threading.Tasks.Task;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayAuraTabViewModel : DisposableReactiveObject, IOverlayAuraTabViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraTabViewModel));

        private readonly SerialDisposable loadedModelAnchors = new SerialDisposable();
        private readonly IFactory<IOverlayAuraModel> auraModelFactory;

        private bool isSelected;
        private OverlayAuraProperties properties;
        private bool isEnabled;
        private bool isActive;
        private ICloseController closeController;
        private IOverlayAuraModel model;
        private string path;
        private string tabName;

        public OverlayAuraTabViewModel(
            OverlayAuraProperties initialProperties,
            [NotNull] IFactory<IPropertyEditorViewModel> propertiesEditorFactory,
            [NotNull] IFactory<IOverlayAuraModel> auraModelFactory)
        {
            this.auraModelFactory = auraModelFactory;
            loadedModelAnchors.AddTo(Anchors);

            GeneralEditor = propertiesEditorFactory.Create();

            Properties = initialProperties.CloneJson();
            Id = properties.Id;
            Path = properties.Path;
            TabName = properties.Name;

            this.WhenAnyProperty(x => x.Path, x => x.TabName)
                .Subscribe(x => RaisePropertyChanged(nameof(FullPath)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Model)
                .Subscribe(x =>
                {
                    GeneralEditor.Value = x;
                    IsEnabled = x != null;
                })
                .AddTo(Anchors);
            
            EnableCommand = CommandWrapper.Create(() => ReloadModelAsync(true));
            DisableCommand = CommandWrapper.Create(() => ReloadModelAsync(false));
            
            ReloadModelAsync(properties.IsEnabled);
        }

        public bool IsActive
        {
            get => isActive;
            private set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            private set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        IAuraModel IAuraViewModel.Model => Model;

        public ICommand EnableCommand { get; }
        
        public ICommand DisableCommand { get; }

        public IPropertyEditorViewModel GeneralEditor { get; }

        public string TabName
        {
            get => tabName;
            set => RaiseAndSetIfChanged(ref tabName, value);
        }

        public string FullPath => System.IO.Path.Combine(Path ?? string.Empty, TabName);

        public bool IsSelected
        {
            get => isSelected;
            set => RaiseAndSetIfChanged(ref isSelected, value);
        }

        public string Id { get; }

        public string Path
        {
            get => path;
            set => RaiseAndSetIfChanged(ref path, value);
        }

        public IOverlayAuraModel Model
        {
            get => model;
            private set => this.RaiseAndSetIfChanged(ref model, value);
        }

        public OverlayAuraProperties Properties
        {
            get => properties;
            private set => this.RaiseAndSetIfChanged(ref properties, value);
        }
        
        public ICloseController CloseController
        {
            get => closeController;
            private set => RaiseAndSetIfChanged(ref closeController, value);
        }
        
        public void SetCloseController(ICloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        private void ReloadModelAsync(bool isEnabled)
        {
            Log.Debug($"[{this}] Re-initializing model");
            Model = ReloadModel(isEnabled);
            Log.Debug($"[{this}] New model: {model}");
        }

        private IOverlayAuraModel ReloadModel(bool isEnabled)
        {
            using var sw = new BenchmarkTimer(isEnabled ? $"[{TabName}({Id})] Loading new model" : $"[{TabName}({Id})] Unloading model", Log, $"{nameof(OverlayAuraTabViewModel)}.{nameof(ReloadModel)}");

            var modelAnchors = new CompositeDisposable().AssignTo(loadedModelAnchors);
            sw.Step($"Disposed previous model");
            
            if (!isEnabled)
            {
                this.WhenAnyValue(x => x.TabName)
                    .Subscribe(x =>
                    {
                        var newProperties = properties.CloneJson();
                        newProperties.Name = x;
                        Properties = newProperties;
                    })
                    .AddTo(modelAnchors);
                
                this.WhenAnyValue(x => x.IsEnabled)
                    .Subscribe(x =>
                    {
                        var newProperties = properties.CloneJson();
                        newProperties.IsEnabled = x;
                        Properties = newProperties;
                    })
                    .AddTo(modelAnchors);
                
                IsActive = false;
                return null;
            }

            var model = auraModelFactory.Create();
            sw.Step($"Created new model: {model}");
            sw.Step($"Initialized model Editor");

            model.AddTo(modelAnchors);
            model.Properties = properties;
            model.IsEnabled = true;
            sw.Step($"Loaded model Properties");

            model.WhenAnyValue(x => x.Name)
                .Subscribe(x => TabName = x)
                .AddTo(modelAnchors);
            
            model.WhenAnyValue(x => x.IsActive)
                .Subscribe(modelIsActive => IsActive = modelIsActive)
                .AddTo(modelAnchors);
            
            model.WhenAnyProperty(x => x.Properties)
                .Subscribe(modelProperties => Properties = model.Properties)
                .AddTo(modelAnchors);
            
            this.WhenAnyValue(x => x.TabName)
                .Subscribe(x => model.Name = x)
                .AddTo(modelAnchors);
            
            this.WhenAnyValue(x => x.Path)
                .Subscribe(x => model.Path = x)
                .AddTo(modelAnchors);
            
            this.WhenAnyValue(x => x.CloseController)
                .Where(x => x != null)
                .Subscribe(x => model.SetCloseController(x))
                .AddTo(modelAnchors);
            sw.Step($"Fully initialized model");

            return model;
        }

        public override string ToString()
        {
            return new {TabName, Id, IsSelected}.ToString();
        }
    }
}