using System;

namespace EyeAuras.Shared
{
    public abstract class AuraActionBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraAction where TAuraProperties : class, IAuraProperties, new()
    {
        private string error;

        public abstract string ActionName { get; }

        public abstract string ActionDescription { get; }

        public string Error
        {
            get => error;
            private set => RaiseAndSetIfChanged(ref error, value);
        }

        public void Execute()
        {
            Error = null;
            try
            {
                ExecuteInternal();
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }
        }
        
        protected abstract void ExecuteInternal();
    }
}