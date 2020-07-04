using System;
using System.IO;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using ReactiveUI;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class ProxyAuraTriggerViewModel : ProxyAuraViewModel, IAuraTrigger
    {
        private string triggerDescription = "";
        private bool isInverted;
        private bool showAdvancedSettings;
        private string triggerName = "Proxy Trigger";

        public ProxyAuraTriggerViewModel()
        {
            this.WhenAnyValue(x => x.IsInverted)
                .Subscribe(() => RaisePropertyChanged(nameof(IsActive)))
                .AddTo(Anchors);
        }

        public string TriggerName
        {
            get => triggerName;
            private set => RaiseAndSetIfChanged(ref triggerName, value);
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

        public TimeSpan? TimeLeftTillNextActivation { get; } = null;
        
        public TimeSpan ActivationTimeout { get; set; }
        
        public double ActivationProgress { get; }
        
        public bool NextIsActiveValue { get; }

        public bool EnableAdvancedSettings
        {
            get => showAdvancedSettings;
            set => RaiseAndSetIfChanged(ref showAdvancedSettings, value);
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            base.LoadProperties(source);

            if (source is ProxyAuraProperties proxyProperties)
            {
                TriggerDescription = $"{proxyProperties.ModuleName} is not loaded yet";
                TriggerName = $"Not Available - {BeautifyProxyName(proxyProperties.ModuleName)} - {BeautifyProxyName(proxyProperties.Metadata.TypeName).Replace("properties", string.Empty, StringComparison.OrdinalIgnoreCase)}";
            }
            else
            {
                TriggerDescription = $"{source.GetType().Name} is not initialized yet";
                TriggerName = $"Not Available - {source.GetType().Name}";
            }
        }

        private static string BeautifyProxyName(string name)
        {
            return name.Contains(".") ? Path.GetExtension(name).TrimStart('.') : name;
        }
    }
}