using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity;

namespace EyeAuras.CsScriptAuras.Actions.ExecuteScript
{
    internal sealed class ExecuteScriptActionEditor : AuraPropertiesEditorBase<ExecuteScriptAction>
    {
        private string liveSourceCode;
        private bool expandEditor;

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
                .Throttle(TimeSpan.FromSeconds(0), bgScheduler)
                .Where(x => Source != null)
                .Subscribe(x => Source.SourceCode = LiveSourceCode)
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
        
        public CommandWrapper ExecuteCommand { get; }
    }
}