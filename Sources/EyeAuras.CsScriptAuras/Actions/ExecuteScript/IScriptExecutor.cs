using System.Collections.Generic;
using System.Threading;
using EyeAuras.Shared;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public interface IScriptExecutor : IDisposableReactiveObject
    {
        IEnumerable<string> Output { get; }
        
        void Execute();

        void SetSharedContext(ISharedContext sharedContext);

        void SetAuraContext(IAuraContext auraContext);

        void SetContainer(IUnityContainer container);
    }
}