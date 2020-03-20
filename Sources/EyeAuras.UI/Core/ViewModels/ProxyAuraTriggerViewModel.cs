using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using ReactiveUI;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class ProxyAuraTriggerViewModel : ProxyAuraViewModel, IAuraTrigger
    {
        private string triggerDescription = "Technical Proxy Trigger";
        public string TriggerName { get; } = "ProxyTrigger";
        private bool isInverted;

        public ProxyAuraTriggerViewModel()
        {
            this.WhenAnyValue(x => x.IsInverted)
                .Subscribe(() => RaisePropertyChanged(nameof(IsActive)))
                .AddTo(Anchors);
        }

        public string TriggerDescription
        {
            get => triggerDescription;
            private set => RaiseAndSetIfChanged(ref triggerDescription, value);
        }

        public bool IsInverted
        {
            get => isInverted;
            set => this.RaiseAndSetIfChanged(ref isInverted, value);
        }

        public bool IsActive => false ^ IsInverted;

        protected override void LoadProperties(IAuraProperties source)
        {
            base.LoadProperties(source);

            var typeDescription = (source is ProxyAuraProperties proxyProperties)
                ? $"{proxyProperties.ModuleName} is not loaded yet"
                : $"{source.GetType().Name} is not initialized yet";
            TriggerDescription = $"Technical Proxy Trigger: {typeDescription}";
        }
    }
}