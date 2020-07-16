using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using EyeAuras.Shared.Sharing.Services;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.Sharing.Services;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.Sharing.ViewModels
{
    internal sealed class ShareMessageBoxViewModel : DisposableReactiveObject
    {
        private readonly IAuraSerializer auraSerializer;
        private readonly IShareProviderRepository repository;
        private bool isOpen;
        private IShareProvider selectedProvider;
        private string content;
        private string statusText;

        public ShareMessageBoxViewModel(
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] IAuraSerializer auraSerializer,
            [NotNull] IShareProviderRepository repository)
        {
            this.auraSerializer = auraSerializer;
            this.repository = repository;
            CloseCommand = CommandWrapper.Create(() => IsOpen = false);

            Observable.Merge(
                    repository.Providers.ToObservableChangeSet().ToUnit(),
                    this.WhenAnyValue(x => x.SelectedProvider).ToUnit())
                .Where(_ => SelectedProvider == null && KnownProviders.Any())
                .Subscribe(() => SelectedProvider = KnownProviders.First())
                .AddTo(Anchors);

            repository.Providers.ToObservableChangeSet().ToUnit()
                .Subscribe(() => RaisePropertyChanged(nameof(IsAvailable)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAvailable)
                .Where(x => IsOpen && !IsAvailable && CloseCommand.CanExecute(null))
                .Subscribe(() => CloseCommand.Execute(null))
                .AddTo(Anchors);
            
            ShowCommand = CommandWrapper.Create<object>(ShowCommandExecuted);
            ShareCommand = CommandWrapper.Create<object>(ShareCommandExecuted);
        }

        private async Task ShareCommandExecuted(object arg)
        {
            if (!(arg is string rawContent))
            {
                return;
            }

            var aurasToUpload = auraSerializer.Deserialize(rawContent);
            if (aurasToUpload?.Length <= 0)
            {
                return;
            }
            
            var provider = selectedProvider;
            if (provider == null)
            {
                return;
            }
            
            IsOpen = true;

            StatusText = "Uploading...";
            try
            {
                await Task.Delay(2000);
                var result = await provider.UploadProperties(aurasToUpload);
                StatusText = $"Uploaded to {result}";
            }
            catch (Exception e)
            {
                StatusText = $"Failed to upload - {e.Message}";
            }
        }

        private void ShowCommandExecuted(object arg)
        {
            if (arg == null)
            {
                return;
            }

            IsOpen = true;
            StatusText = null;
            var data = auraSerializer.Serialize(arg);
            Content = data;
        }

        public CommandWrapper CloseCommand { get; }
        
        public CommandWrapper ShowCommand { get; }
        
        public CommandWrapper ShareCommand { get; }

        public ReadOnlyObservableCollection<IShareProvider> KnownProviders => repository.Providers;

        public bool IsAvailable => KnownProviders.Any(); 

        public IShareProvider SelectedProvider
        {
            get => selectedProvider;
            set => RaiseAndSetIfChanged(ref selectedProvider, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }

        public string Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => RaiseAndSetIfChanged(ref statusText, value);
        }
    }
}