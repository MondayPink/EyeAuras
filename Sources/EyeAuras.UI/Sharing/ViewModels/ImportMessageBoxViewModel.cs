using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using EyeAuras.Shared.Sharing.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Sharing.Services;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.Sharing.ViewModels
{
    internal sealed class ImportMessageBoxViewModel : DisposableReactiveObject
    {
        private readonly IGlobalContext globalContext;
        private readonly IClipboardManager clipboardManager;
        private readonly IAuraSerializer auraSerializer;
        private readonly IShareProviderRepository repository;
        private bool isOpen;
        private OverlayAuraProperties[] content;
        private string statusText;
        private string contentUri;

        public ImportMessageBoxViewModel(
            [NotNull] IGlobalContext globalContext,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] IAuraSerializer auraSerializer,
            [NotNull] IAuraPreviewViewModel auraPreview,
            [NotNull] IShareProviderRepository repository)
        {
            this.globalContext = globalContext;
            this.clipboardManager = clipboardManager;
            this.auraSerializer = auraSerializer;
            this.repository = repository;
            CloseCommand = CommandWrapper.Create(() => IsOpen = false);
            AuraPreview = auraPreview.AddTo(Anchors);

            repository.Providers.ToObservableChangeSet().ToUnit()
                .Subscribe(() => RaisePropertyChanged(nameof(IsAvailable)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAvailable)
                .Where(x => IsOpen && !IsAvailable && CloseCommand.CanExecute(null))
                .Subscribe(() => CloseCommand.Execute(null))
                .AddTo(Anchors);
            
            ShowCommand = CommandWrapper.Create<object>(ShowCommandExecuted);
            ImportCommand = CommandWrapper.Create<object>(ImportCommandExecuted);
            DownloadCommand = CommandWrapper.Create<object>(DownloadCommandExecuted);

            this.WhenAnyValue(x => x.Content)
                .Subscribe(x => auraPreview.Content = x)
                .AddTo(Anchors);
        }

        public IAuraPreviewViewModel AuraPreview { get; }
        
        public CommandWrapper CloseCommand { get; }
        
        public CommandWrapper ShowCommand { get; }
        
        public CommandWrapper ImportCommand { get; }
        
        public CommandWrapper DownloadCommand { get; }
        
        public ReadOnlyObservableCollection<IShareProvider> KnownProviders => repository.Providers;

        public bool IsAvailable => KnownProviders.Any();

        public string ContentUri
        {
            get => contentUri;
            set => RaiseAndSetIfChanged(ref contentUri, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }

        public OverlayAuraProperties[] Content
        {
            get => content;
            private set => RaiseAndSetIfChanged(ref content, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => RaiseAndSetIfChanged(ref statusText, value);
        }

        private void ImportCommandExecuted(object arg)
        {
            if (!(arg is OverlayAuraProperties[] auraProperties))
            {
                return;
            }

            Reset();
            globalContext.CreateAura(auraProperties);
            if (CloseCommand.CanExecute(null))
            {
                CloseCommand.Execute(null);
            }
        }

        private async Task DownloadCommandExecuted(object arg)
        {
            if (arg == null)
            {
                return;
            }

            Reset();
            try
            {
                if (Uri.TryCreate(arg as string, UriKind.Absolute, out var uriArg))
                {
                    ContentUri = uriArg.ToString();
                    await Task.Delay(5000);
                    foreach (var provider in KnownProviders)
                    {
                        var result = (await provider.DownloadProperties(uriArg)).EmptyIfNull().OfType<OverlayAuraProperties>().ToArray();
                        if (!result.Any())
                        {
                            continue;
                        }

                        Content = result;
                        ContentUri = null;
                        break;
                    }
                }
                else if (arg is string stringArg)
                {
                    Content = auraSerializer.Deserialize(stringArg);
                }

                if (content == null || content.Length == 0)
                {
                    throw new FormatException($"Failed to process {arg}");
                }
            }
            catch (Exception e)
            {
                ContentUri = string.IsNullOrWhiteSpace(arg as string) ? "<Clipboard is empty>" : arg as string;
                throw new FormatException($"Parsing error - {e.Message}", e);
            }
        }

        private void ShowCommandExecuted(object arg)
        {
            Reset();
            switch (arg)
            {
                case string stringArg:
                    ContentUri = stringArg;
                    break;
                case OverlayAuraProperties[] propertiesArg:
                    Content = propertiesArg;
                    break;
                default:
                    ContentUri = clipboardManager.GetText();
                    break;
            }
        }

        private void Reset()
        {
            IsOpen = true;
            StatusText = null;
            Content = null;
            ContentUri = null;
        }
    }
}