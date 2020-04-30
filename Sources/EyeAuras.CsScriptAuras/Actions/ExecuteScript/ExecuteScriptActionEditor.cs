using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.PLinq;
using EyeAuras.Shared;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptActionEditor : AuraPropertiesEditorBase<ExecuteScriptAction>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExecuteScriptActionEditor));

        private string liveSourceCode;
        private bool expandEditor;
        private ReadOnlyObservableCollection<KeyValuePair<string, string>> auraVariables;
        private bool showOutput;

        public ExecuteScriptActionEditor(
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            ExecuteCommand = CommandWrapper.Create(
                async () =>
                {
                    if (Source == null)
                    {
                        return;
                    }
                    await Task.Run(Source.Execute);
                }, 
                this.WhenAnyValue(x => x.Source.State).Select(x => x == ScriptState.ReadyToRun).ObserveOn(uiScheduler));
            this.WhenAnyValue(x => x.Source.SourceCode)
                .Subscribe(x => LiveSourceCode = x)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.LiveSourceCode)
                .Skip(1)
                .Where(x => Source != null)
                .DistinctUntilChanged(x => x?.Trim())
                .Throttle(TimeSpan.FromSeconds(0), bgScheduler)
                .Subscribe(x => Source.SourceCode = LiveSourceCode)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Source.Context.Variables)
                .Select(sourceVariables =>
                {
                    return Observable.Create<ReadOnlyObservableCollection<KeyValuePair<string, string>>>(observer =>
                    {
                        var chain = sourceVariables.Connect()
                            .Transform(x =>
                                new KeyValuePair<string, string>(x.Key, x.Value == null ? "null" : x.Value.ToString()))
                            .ObserveOn(uiScheduler)
                            .Bind(out var result)
                            .Subscribe(() => { }, Log.HandleUiException);
                        observer.OnNext(result);
                        return chain;
                    });
                })
                .Switch()
                .Subscribe(x =>
                {
                    AuraVariables = x;
                })
                .AddTo(Anchors);
        }

        public string LiveSourceCode
        {
            get => liveSourceCode;
            set => this.RaiseAndSetIfChanged(ref liveSourceCode, value);
        }
        
        public bool ExpandEditor
        {
            get => expandEditor;
            set => this.RaiseAndSetIfChanged(ref expandEditor, value);
        }

        public bool ShowOutput
        {
            get => showOutput;
            set => RaiseAndSetIfChanged(ref showOutput, value);
        }
        
        public CommandWrapper ExecuteCommand { get; }

        public ReadOnlyObservableCollection<KeyValuePair<string, string>> AuraVariables
        {
            get => auraVariables;
            private set => RaiseAndSetIfChanged(ref auraVariables, value);
        }
    }
}