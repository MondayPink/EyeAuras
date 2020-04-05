using System;
using EyeAuras.Shared;
using log4net;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptAction : AuraActionBase<ExecuteScriptActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExecuteScriptAction));
        private string sourceCode;
        private string error;

        public ExecuteScriptAction()
        {
        }

        protected override void VisitLoad(ExecuteScriptActionProperties source)
        {
            SourceCode = source.SourceCode;
        }

        protected override void VisitSave(ExecuteScriptActionProperties source)
        {
            source.SourceCode = SourceCode;
        }

        public string Error
        {
            get => error;
            private set => this.RaiseAndSetIfChanged(ref error, value);
        }

        public string SourceCode
        {
            get => sourceCode;
            set => this.RaiseAndSetIfChanged(ref sourceCode, value);
        }

        public override string ActionName { get; } = "C# Script";
        
        public override string ActionDescription { get; } = "Executes arbitrary C# code";
        
        public override void Execute()
        {
            try
            {

            }
            catch (Exception e)
            {
                Log.Warn($"Error executing script - {e.ToString()}\n{SourceCode}");
                Error = e.ToString();
            }
        }
    }
}