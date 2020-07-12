using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Dragablz;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Utilities;
using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayReplicaCoreEditorViewModel : AuraPropertiesEditorBase<OverlayReplicaAuraCore>
    {
    }
}