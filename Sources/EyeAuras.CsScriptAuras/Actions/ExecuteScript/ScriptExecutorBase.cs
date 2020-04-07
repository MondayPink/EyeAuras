using System.Text;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    public abstract class ScriptExecutorBase : IScriptExecutor 
    {
        public StringBuilder Output { get; } = new StringBuilder();
        
        public abstract void Execute();

        public void LogDebug(string message)
        {
            Output.AppendLine($"[DEBUG] {message}");
        }
    }
}