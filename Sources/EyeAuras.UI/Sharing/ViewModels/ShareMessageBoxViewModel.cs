using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

            this.WhenAnyValue(x => x.SelectedProvider)
                .Where(x => x == null && KnownProviders.Any())
                .Subscribe(() => SelectedProvider = KnownProviders.First())
                .AddTo(Anchors);
            
            ShowCommand = CommandWrapper.Create<object>(ShowCommandExecuted);
            ShareCommand = CommandWrapper.Create<object>(ShareCommandExecuted);
        }

        private async Task ShareCommandExecuted(object arg)
        {
            if (arg == null)
            {
                return;
            }

            if (SelectedProvider == null)
            {
                return;
            }
            
            IsOpen = true;

            StatusText = "Uploading...";
            try
            {
                await Task.Delay(5000);
                throw new NotImplementedException();
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