using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicData;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Modularity;
using PoeShared.UI.TreeView;
using PoeShared.Wpf.Scaffolding;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.MainWindow.Models
{
    internal sealed class AuraSerializer : IAuraSerializer
    {
        private readonly IConfigSerializer configSerializer;

        public AuraSerializer(IConfigSerializer configSerializer)
        {
            this.configSerializer = configSerializer;
        }

        public string Serialize(object parameter)
        {
            object dataToSerialize = parameter switch
            {
                OverlayAuraProperties aura => new[] { aura },
                OverlayAuraProperties[] auras => auras,
                IAuraTabViewModel auraTab => new[] { auraTab.Properties },
                HolderTreeViewItemViewModel treeItemForAura => new[] { treeItemForAura.Value.Properties },
                DirectoryTreeViewItemViewModel treeDirectory => treeDirectory
                    .FindChildrenOfType<HolderTreeViewItemViewModel>()
                    .Select(x => x.Value)
                    .OfType<IAuraTabViewModel>()
                    .Select(x => x.Properties)
                    .ToArray(),
                var _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter,
                    $"Something went wrong - failed to copy parameter of type {parameter.GetType()}: {parameter}")
            };

            var data = configSerializer.Serialize(dataToSerialize);
            return data;
        }

        public OverlayAuraProperties[] Deserialize(string content)
        {
            content = content.Trim();
            return configSerializer.DeserializeSingleOrList<OverlayAuraProperties>(content).Where(x => x.IsValid).ToArray();
        }
    }
}