using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IUniqueIdGenerator
    {
        [NotNull]
        string Next();
    }
}