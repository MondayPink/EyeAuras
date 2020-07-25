using EyeAuras.Shared;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class EmptyAuraCore : AuraCore<EmptyCoreProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EmptyAuraCore));

        public EmptyAuraCore()
        {
            using var sw = new BenchmarkTimer($"[{this}] {nameof(EmptyAuraCore)} initialization", Log, nameof(OverlayAuraModel));
            sw.Step($"Empty core registration completed");
        }

        protected override void VisitSave(EmptyCoreProperties source)
        {
        }

        protected override void VisitLoad(EmptyCoreProperties source)
        {
        }

        public override string Name { get; } = "\uf05e No overlay";
        
        public override string Description { get; } = "Do not show overlay, still process Actions and Triggers";
    }
}