using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSScriptLib;
using EyeAuras.Shared;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Stateless;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptAction : AuraActionBase<ExecuteScriptActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExecuteScriptAction));
        
        private string sourceCode;
        private string error;
        private string scriptCode;
        private IScriptExecutor scriptExecutor;
        private ScriptState state;

        public ExecuteScriptAction([NotNull] [Unity.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.WhenAnyValue(x => x.SourceCode)
                .Subscribe(async () => await PrepareExecutorAsync())
                .AddTo(Anchors);
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

        public string ScriptCode
        {
            get => scriptCode;
            private set => this.RaiseAndSetIfChanged(ref scriptCode, value);
        }

        public IScriptExecutor ScriptExecutor
        {
            get => scriptExecutor;
            private set => this.RaiseAndSetIfChanged(ref scriptExecutor, value);
        }

        public override string ActionName { get; } = "C# Script";
        
        public override string ActionDescription { get; } = "Executes arbitrary C# code";

        public ScriptState State
        {
            get => state;
            set => this.RaiseAndSetIfChanged(ref state, value);
        }

        public override void Execute()
        {
            try
            {
                if (State != ScriptState.ReadyToRun)
                {
                    return;
                }

                Guard.ArgumentNotNull(() => scriptExecutor);

                Error = null;
                State = ScriptState.Running;

                scriptExecutor.Execute();
            }
            catch (Exception e)
            {
                Log.Warn($"Error executing script - {e}\n{SourceCode}");
                Error = e.ToString();
            }
            finally
            {
                State = ScriptState.ReadyToRun;
            }
        }

        private string PrepareScriptCode()
        {
            var usings = new List<string>()
            {
                "System.Collections.Generic",
                "System.Windows",
                typeof(IScriptExecutor).Namespace,
                typeof(ScriptExecutorBase).Namespace,
                typeof(Process).Namespace,
                typeof(ObjectExtensions).Namespace,
                typeof(Enumerable).Namespace,
                typeof(Dictionary<,>).Namespace,
                typeof(Array).Namespace,
                typeof(ArrayList).Namespace,
                typeof(List<>).Namespace
            };
            var scriptCode =  @"%USINGS%
                             public class Script : ScriptExecutorBase
                             {
                                 public override void Execute()
                                 {
                                    %SCRIPTCODE%
                                 }
                             }";
            scriptCode = scriptCode.Replace("%USINGS%", string.Join(Environment.NewLine, usings.Distinct().Select(x => $"using {x};")));
            scriptCode = scriptCode.Replace("%SCRIPTCODE%", SourceCode);
            return scriptCode;
        }

        private async Task PrepareExecutorAsync()
        {
            try
            {
                Error = null;
                State = ScriptState.Compiling;
                ScriptCode = PrepareScriptCode();
                var sourceToCompile = SourceCode;
                var fullScriptToCompile = ScriptCode;
                var executor = await Task.Run(
                    () =>
                    {
                        Log.Debug($"Compiling code '{sourceToCompile?.Take(50)}'");
                        return CSScript.Evaluator.LoadCode<IScriptExecutor>(fullScriptToCompile);
                    });
                
                if (sourceToCompile != sourceCode)
                {
                    Log.Debug($"Code changed during compilation, discarding compiled code");
                    return;
                }
                ScriptExecutor = executor;
                State = ScriptState.ReadyToRun;
                Error = null;
            }
            catch (Exception e)
            {
                ScriptExecutor = null;
                State = ScriptState.NotReady;
                Log.Warn($"Error compiling script - {e}\n{SourceCode}");
                Error = e.ToString();
            }
        }
    }
}