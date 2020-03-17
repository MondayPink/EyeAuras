namespace EyeAuras.Shared
{
    public abstract class AuraActionBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraAction where TAuraProperties : class, IAuraProperties, new()
    {
        public abstract string ActionName { get; }

        public abstract string ActionDescription { get; }

        public abstract void Execute();
    }
}