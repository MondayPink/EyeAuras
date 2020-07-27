using System;

namespace EyeAuras.Shared
{
    public abstract class AuraActionBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraAction where TAuraProperties : class, IAuraProperties, new()
    {
        private string error;
        private bool isBusy;

        public abstract string ActionName { get; }

        public abstract string ActionDescription { get; }

        public string Error
        {
            get => error;
            protected set => RaiseAndSetIfChanged(ref error, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set => RaiseAndSetIfChanged(ref isBusy, value);
        }
        
        public void Execute()
        {
            Error = null;
            IsBusy = true;
            try
            {
                ExecuteInternal();
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }
            finally{
            
                IsBusy = false;
            }
        }
        
        protected abstract void ExecuteInternal();
        
        public override string ToString()
        {
            return ActionName;
        }
    }
}