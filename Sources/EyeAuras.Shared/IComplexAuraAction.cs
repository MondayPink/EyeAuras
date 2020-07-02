using DynamicData;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IComplexAuraAction : ISourceList<IAuraAction>, IAuraAction
    {
        void Add([NotNull] IAuraAction action);

        bool Remove([NotNull] IAuraAction action);
    }
}