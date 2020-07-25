using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using log4net;
using Microsoft.Win32;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayImageCoreEditorViewModel : AuraPropertiesEditorBase<OverlayImageAuraCore>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayImageCoreEditorViewModel));

        private readonly ObservableAsPropertyHelper<BitmapSource> imagePreview;

        public OverlayImageCoreEditorViewModel([NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            OpenImageFileSelectorCommand = CommandWrapper.Create(OpenImageFileSelectorCommandExecuted);

            imagePreview =
                this.WhenAnyValue(x => x.Source)
                    .Select(
                        () => Source != null
                            ? Source.WhenAnyValue(x => x.ImageFile)
                            : Observable.Return(default(BitmapSource)))
                    .Switch()
                    .ToPropertyHelper(this, x => x.ImageFilePreview, uiScheduler)
                    .AddTo(Anchors);
            
            ResetImageCommand = CommandWrapper.Create(() => Source.ResetImage(), this.WhenAnyValue(x => x.Source).Select(x => x != null));
        }
        
        public BitmapSource ImageFilePreview => imagePreview.Value;

        public CommandWrapper OpenImageFileSelectorCommand { get; }
        
        public CommandWrapper ResetImageCommand { get; }
        
        private void OpenImageFileSelectorCommandExecuted()
        {
            Log.Info($"Showing OpenFileDialog to user, current file path: {Source.ImageFilePath}");

            var initialDirectory = Path.GetDirectoryName(Source.ImageFilePath);
            var op = new OpenFileDialog
            {
                Title = "Select an image", 
                FileName = Source.ImageFilePath,
                InitialDirectory = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory) 
                    ? initialDirectory
                    : Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                CheckPathExists = true,
                Multiselect = false,
                Filter = "All supported graphics|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*"
            };

            if (op.ShowDialog() == true)
            {
                Source.ImageFilePath = op.FileName;
            }  
        }
    }
}