using EyeAuras.Shared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.DefaultAuras.Triggers.Default
{
    public sealed class DefaultTrigger : AuraTriggerBase<DefaultTriggerProperties>
    {
        public override string TriggerName { get; } = "Fixed Value Trigger";

        public override string TriggerDescription { get; } = "Trigger that is always True or False";

        public DefaultTrigger()
        {
            RaisePropertiesWhen(this.WhenAnyProperty(x => x.TriggerValue));
        }
        
        protected override void VisitLoad(DefaultTriggerProperties source)
        {
            base.VisitLoad(source);
            TriggerValue = source.TriggerValue;
        }

        protected override void VisitSave(DefaultTriggerProperties source)
        {
            base.VisitSave(source);
            source.TriggerValue = TriggerValue;
        }
    }
}