using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DynamicData;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.MainWindow.ViewModels;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.UI.TreeView;
using ReactiveUI;

namespace EyeAuras.UI.Sharing.ViewModels
{
    internal sealed class AuraPreviewViewModel : DisposableReactiveObject, IAuraPreviewViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraPreviewViewModel));

        private OverlayAuraProperties[] content;
        private readonly SourceList<ITreeViewItemViewModel> previewData = new SourceList<ITreeViewItemViewModel>();

        public AuraPreviewViewModel()
        {
            previewData
                .Connect()
                .Bind(out var previewDataSource)
                .Subscribe()
                .AddTo(Anchors);
            PreviewData = previewDataSource;

            this.WhenAnyValue(x => x.Content)
                .Subscribe(RebuildPreview, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public OverlayAuraProperties[] Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }
        
        public ReadOnlyObservableCollection<ITreeViewItemViewModel> PreviewData { get; }

        private void RebuildPreview()
        {
            previewData.Clear();

            var auras = content.EmptyIfNull().Select(x => new TextEyeTreeItemViewModel(null)
            {
                Name = Path.Combine(x.Path ?? string.Empty, x.Name)
            });

            auras.ForEach(previewData.Add);
        }
    }
}