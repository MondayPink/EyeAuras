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
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptAction : AuraActionBase<ExecuteScriptActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExecuteScriptAction));

        private readonly ISharedContext sharedContext;
        private readonly IUnityContainer container;
        
        private string sourceCode;
        private string scriptCode;
        private IScriptExecutor scriptExecutor;
        private ScriptState state;
        private string scriptLog;

        public ExecuteScriptAction(
            ISharedContext sharedContext,
            IUnityContainer container,
            [NotNull] [Unity.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.sharedContext = sharedContext;
            this.container = container;
            this.WhenAnyValue(x => x.SourceCode)
                .Subscribe(async () => await PrepareExecutorAsync(), Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ScriptExecutor)
                .Select(x => x == null ? Observable.Return(default(string)) : x.WhenAnyProperty(y => y.Output).StartWithDefault().Select(y => string.Join(Environment.NewLine, x.Output.Where(record => !string.IsNullOrEmpty(record)))))
                .Switch()
                .Subscribe(x => ScriptLog = x, Log.HandleUiException)
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

        public string SourceCode
        {
            get => sourceCode;
            set => this.RaiseAndSetIfChanged(ref sourceCode, value);
        }

        public string ScriptLog
        {
            get => scriptLog;
            private set => RaiseAndSetIfChanged(ref scriptLog, value);
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
        
        public override string ActionDescription { get; } = "Executes arbitrary C# code - EARLY ALPHA USE WITH CARE";

        public ScriptState State
        {
            get => state;
            set => this.RaiseAndSetIfChanged(ref state, value);
        }

        protected override void ExecuteInternal()
        {
            try
            {
                if (State != ScriptState.ReadyToRun)
                {
                    return;
                }

                Guard.ArgumentNotNull(() => scriptExecutor);

                State = ScriptState.Running;

                scriptExecutor.Execute();
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error executing script - {e}\n{SourceCode}", e);
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
                typeof(ISharedContext).Namespace,
                typeof(IAuraContext).Namespace,
                typeof(IObservableCache<,>).Namespace,
                typeof(IUnityContainer).Namespace,
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
                             public sealed class Script : ScriptExecutorBase
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

                executor.SetAuraContext(Context);
                executor.SetSharedContext(sharedContext);
                executor.SetContainer(container);
                ScriptExecutor = executor;
                State = ScriptState.ReadyToRun;
            }
            catch (Exception e)
            {
                ScriptExecutor = null;
                State = ScriptState.NotReady;
                Log.Warn($"Error compiling script - {e}\n{SourceCode?.Trim()}");
                Error = e.ToString();
            }
        }
    }
}