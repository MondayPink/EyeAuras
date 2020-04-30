using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public interface IAuraModel : IDisposableReactiveObject
    {
        IAuraProperties Properties { [NotNull] get; [NotNull] set; }

        IAuraContext Context { [CanBeNull] get; [CanBeNull] set; }
    }

    public interface IAuraModel<T> : IAuraModel where T : class, IAuraProperties
    {
        new T Properties { [NotNull] get; [NotNull] set; }
    }
}