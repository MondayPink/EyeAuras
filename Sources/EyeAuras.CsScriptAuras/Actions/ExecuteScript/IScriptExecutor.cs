using System.Collections.Generic;
using EyeAuras.Shared;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public interface IScriptExecutor : IDisposableReactiveObject
    {
        IEnumerable<string> Output { get; }
        
        void Execute();

        void SetContext(ISharedContext sharedContext);

        void SetContainer(IUnityContainer container);
    }
}