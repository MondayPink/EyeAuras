using System;
using System.IO;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class ProxyAuraActionViewModel : ProxyAuraViewModel, IAuraAction
    {
        private string actionDescription = "";
        private string actionName = "Proxy Action";

        public string ActionDescription
        {
            get => actionDescription;
            private set => RaiseAndSetIfChanged(ref actionDescription, value);
        }

        public string ActionName
        {
            get => actionName;
            set => RaiseAndSetIfChanged(ref actionName, value);
        }

        public string Error { get; }
        
        public bool IsBusy { get; } = false;

        public void Execute()
        {
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            base.LoadProperties(source);
            
            if (source is ProxyAuraProperties proxyProperties)
            {
                ActionDescription = $"{proxyProperties.ModuleName} is not loaded yet";
                ActionName = $"Not Available - {BeautifyProxyName(proxyProperties.ModuleName)} - {BeautifyProxyName(proxyProperties.Metadata.TypeName).Replace("properties", string.Empty, StringComparison.OrdinalIgnoreCase)}";
            }
            else
            {
                ActionDescription = $"{source.GetType().Name} is not initialized yet";
                ActionName = $"Not Available - {source.GetType().Name}";
            }
        }
        
        private static string BeautifyProxyName(string name)
        {
            return name.Contains(".") ? Path.GetExtension(name).TrimStart('.') : name;
        }
    }
}