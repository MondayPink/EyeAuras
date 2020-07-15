using System.Reactive.Disposables;
using EyeAuras.Shared;
using PoeShared.Audio.ViewModels;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.IO;
using log4net;
using Microsoft.Win32;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding.WPF;

namespace EyeAuras.DefaultAuras.Actions.PlaySound
{
    internal sealed class PlaySoundActionEditor : AuraPropertiesEditorBase<PlaySoundAction>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaySoundActionEditor));

        private readonly IAudioNotificationsManager notificationsManager;

        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();
        private string lastOpenedDirectory;
        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public PlaySoundActionEditor(
            IAudioNotificationsManager notificationsManager,
            IAudioNotificationSelectorViewModel audioNotificationSelector)
        {
            this.notificationsManager = notificationsManager;
            AudioNotificationSelector = audioNotificationSelector.AddTo(Anchors);
            activeSourceAnchors.AddTo(Anchors);

            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
            
            AddSoundCommand = CommandWrapper.Create(AddSoundCommandExecuted);
        }
        
        public CommandWrapper AddSoundCommand { get; }

        public string LastOpenedDirectory
        {
            get => lastOpenedDirectory;
            set => RaiseAndSetIfChanged(ref lastOpenedDirectory, value);
        }

        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Source.WhenAnyValue(x => x.Notification).Subscribe(x => AudioNotificationSelector.SelectedValue = x).AddTo(sourceAnchors);
            AudioNotificationSelector.WhenAnyValue(x => x.SelectedValue).Subscribe(x => Source.Notification = x).AddTo(sourceAnchors);
        }
        
        private void AddSoundCommandExecuted()
        {
            Log.Info($"Showing OpenFileDialog to user");

            var op = new OpenFileDialog
            {
                Title = "Select an image", 
                InitialDirectory = !string.IsNullOrEmpty(lastOpenedDirectory) && Directory.Exists(lastOpenedDirectory) 
                    ? lastOpenedDirectory
                    : Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                CheckPathExists = true,
                Multiselect = false,
                Filter = "All supported sound files|*.wav;*.mp3|All files|*.*"
            };

            if (op.ShowDialog() != true)
            {
                return;
            }

            Log.Debug($"Adding notification {op.FileName}");
            LastOpenedDirectory = Path.GetDirectoryName(op.FileName);
            var notification = notificationsManager.AddFromFile(new FileInfo(op.FileName));
            Source.Notification = notification;
        }
    }
}