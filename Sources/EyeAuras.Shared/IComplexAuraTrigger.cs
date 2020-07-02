using System.Collections.ObjectModel;
using DynamicData;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IComplexAuraTrigger : ISourceList<IAuraTrigger>, IAuraTrigger
    {
        void Add([NotNull] IAuraTrigger trigger);

        bool Remove([NotNull] IAuraTrigger trigger);
    }
}