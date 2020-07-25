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
using PoeShared.Modularity;
using Stateless;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptAction : AuraActionBase<ExecuteScriptActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExecuteScriptAction));

        private readonly ISharedContext sharedContext;
        private readonly IUnityContainer container;

        private string sourceCode;
        private IScriptExecutor scriptExecutor;
        private string scriptLog;

        private readonly StateMachine<ScriptState, ScriptTrigger> scriptStateMachine;

        private readonly StateMachine<ScriptState, ScriptTrigger>.TriggerWithParameters<IScriptExecutor> codeReadyTrigger;
        private readonly StateMachine<ScriptState, ScriptTrigger>.TriggerWithParameters<string> codeChangeTrigger;
        
        public ExecuteScriptAction(
            ISharedContext sharedContext,
            IUnityContainer container,
            [NotNull] [Unity.Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Unity.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.sharedContext = sharedContext;
            this.container = container;

            scriptStateMachine = new StateMachine<ScriptState, ScriptTrigger>(ScriptState.NotReady);
            codeChangeTrigger = scriptStateMachine.SetTriggerParameters<string>(ScriptTrigger.CodeChange);
            codeReadyTrigger = scriptStateMachine.SetTriggerParameters<IScriptExecutor>(ScriptTrigger.Compile);
            
            scriptStateMachine.OnTransitioned(x => this.RaisePropertyChanged(nameof(State)));
            scriptStateMachine.OnTransitioned(x => Log.Debug($"[{Context?.Id} {Context?.Name}] Transitioning to {x.Destination} from {x.Source} via {x.Trigger}"));
            scriptStateMachine.OnUnhandledTrigger((scriptState, trigger) => Log.Warn($"[{Context?.Id} {Context?.Name}] Failed to change state from {scriptStateMachine.State} to {scriptState} via trigger {trigger}"));

            scriptStateMachine.Configure(ScriptState.NotReady)
                .Permit(ScriptTrigger.CodeChange, ScriptState.Compiling);
            
            scriptStateMachine.Configure(ScriptState.Compiling)
                .Permit(ScriptTrigger.Initialization, ScriptState.NotReady)
                .Permit(codeReadyTrigger.Trigger, ScriptState.ReadyToRun)
                .OnEntryFrom(codeChangeTrigger, userCode =>
                {
                    ScriptExecutor = null;
                    PrepareExecutor(userCode);
                })
                .PermitReentry(codeChangeTrigger.Trigger);

            scriptStateMachine.Configure(ScriptState.ReadyToRun)
                .Permit(codeChangeTrigger.Trigger, ScriptState.Compiling)
                .OnEntryFrom(codeReadyTrigger, executor => ScriptExecutor = executor);

            this.WhenAnyValue(x => x.ScriptExecutor)
                .Select(x => x == null ? Observable.Return(default(string)) : x.WhenAnyProperty(y => y.Output).StartWithDefault().Select(y => string.Join(Environment.NewLine, x.Output.Where(record => !string.IsNullOrEmpty(record)))))
                .Switch()
                .Subscribe(x => ScriptLog = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.SourceCode)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromSeconds(1), bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(async x =>
                {
                    await scriptStateMachine.FireAsync(codeChangeTrigger, x);
                }, Log.HandleUiException)
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

        [AuraProperty]
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

        public IScriptExecutor ScriptExecutor
        {
            get => scriptExecutor;
            private set => this.RaiseAndSetIfChanged(ref scriptExecutor, value);
        }

        public override string ActionName { get; } = "C# Script";
        
        public override string ActionDescription { get; } = "Executes arbitrary C# code - EARLY ALPHA USE WITH CARE";

        public ScriptState State => scriptStateMachine.State;

        protected override void ExecuteInternal()
        {
            try
            {
                var executor = ScriptExecutor;
                if (executor == null)
                {
                    return;
                }

                executor.Execute();
            }
            catch (Exception e)
            {
                var error = new ApplicationException($"Error executing script - {e}\n{SourceCode}", e);
                Error = $"Runtime Error - {error}";
            }
        }

        private static string PrepareScriptCode(string userCode)
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
            scriptCode = scriptCode.Replace("%SCRIPTCODE%", userCode);
            return scriptCode;
        }
        
        private void PrepareExecutor(string userCode)
        {
            try
            {
                var scriptCode = PrepareScriptCode(userCode);
                var fullScriptToCompile = scriptCode;
                Log.Debug($"[{Context.Id} {Context.Name}] Compiling code '{userCode?.Take(50)}'");
                var executor = CSScript.Evaluator.LoadCode<IScriptExecutor>(fullScriptToCompile);

                executor.SetAuraContext(Context);
                executor.SetSharedContext(sharedContext);
                executor.SetContainer(container);

                if (userCode == sourceCode)
                {
                    scriptStateMachine.Fire(codeReadyTrigger, executor);
                }
                else
                {
                    scriptStateMachine.Fire(ScriptTrigger.Initialization);
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Error compiling script - {e}\n{SourceCode?.Trim()}");
                Error = $"Compilation Error - {e}";
                scriptStateMachine.Fire(ScriptTrigger.Initialization);
            }
        }

        private enum ScriptTrigger
        {
            Initialization,
            CodeChange,
            Compile,
        }
    }
}