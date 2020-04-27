using EyeAuras.Shared;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptActionProperties : IAuraProperties
    {
        public string SourceCode { get; set; }
        
        public int Version { get; set; } = 1;
    }
}