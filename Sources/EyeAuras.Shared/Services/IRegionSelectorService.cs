using System;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IRegionSelectorService
    {
        [NotNull] 
        IObservable<RegionSelectorResult> SelectRegion();
    }
}