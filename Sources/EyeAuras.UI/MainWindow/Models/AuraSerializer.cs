using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.ViewModels;
using PoeShared.Modularity;
using PoeShared.UI.TreeView;

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
                IAuraTabViewModel auraTab => auraTab.Properties,
                HolderTreeViewItemViewModel treeItemForAura => treeItemForAura.Value.Properties,
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

            var tabsToPaste = new List<OverlayAuraProperties>();
            try
            {
                var cfg = configSerializer.Deserialize<OverlayAuraProperties>(content);
                tabsToPaste.Add(cfg);
            } catch { }
                
            try
            {
                var cfg = configSerializer.Deserialize<List<OverlayAuraProperties>>(content);
                tabsToPaste.Add(cfg);
            } catch { }
            
            if (tabsToPaste.Count == 0)
            {
                throw new FormatException($"Failed to paste clipboard content");
            }

            return tabsToPaste.ToArray();
        }
    }
}