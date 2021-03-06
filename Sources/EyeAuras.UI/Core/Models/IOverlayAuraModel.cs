using System.Collections.ObjectModel;
using DynamicData.Annotations;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Overlay.ViewModels;
using PoeShared.Native;

namespace EyeAuras.UI.Core.Models
{
    internal interface IOverlayAuraModel : IAuraModel<OverlayAuraProperties>, IAuraModelController
    {   
        string Id { [NotNull] get; }

        bool IsActive { get; }
        
        IAuraCore Core { get; }

        IComplexAuraTrigger Triggers { [NotNull] get; }
        
        IComplexAuraAction OnEnterActions { get; }
        
        IComplexAuraAction OnExitActions { get; }
        
        IComplexAuraAction WhileActiveActions { get; }
    }
}