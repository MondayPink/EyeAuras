using System.Windows;
using EyeAuras.Shared;
using log4net;
using PoeShared.Audio.Services;

namespace EyeAuras.DefaultAuras.Actions.PlaySound
{
    public sealed class PlaySoundAction : AuraActionBase<PlaySoundActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaySoundAction));

        private readonly IAudioNotificationsManager notificationsManager;
        private string notification;

        public PlaySoundAction(IAudioNotificationsManager notificationsManager)
        {
            this.notificationsManager = notificationsManager;
            Notification = AudioNotificationType.Ping.ToString();
        }

        public string Notification
        {
            get => notification;
            set => this.RaiseAndSetIfChanged(ref notification, value);
        }

        protected override void VisitLoad(PlaySoundActionProperties source)
        {
            Notification = source.Notification;
        }

        protected override void VisitSave(PlaySoundActionProperties source)
        {
            source.Notification = notification;
        }

        public override string ActionName { get; } = "Play Sound";
        
        public override string ActionDescription { get; } = "plays specified sound";
        
        protected override void ExecuteInternal()
        {
            if (string.IsNullOrEmpty(notification))
            {
                return;
            }
            Log.Debug($"Playing notification {notification}");
            notificationsManager.PlayNotification(notification);
        }
    }
}