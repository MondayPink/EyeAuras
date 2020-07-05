using EyeAuras.Shared;

namespace EyeAuras.UI.Core.Models
{
    internal abstract class AuraCore<T> : AuraModelBase<T>, IAuraCore where T : class, IAuraCoreProperties, new()
    {
        private IAuraModelController modelController;

        public abstract string Name { get; }
        
        public abstract string Description { get; }

        public IAuraModelController ModelController
        {
            get => modelController;
            set => RaiseAndSetIfChanged(ref modelController, value);
        }
    }
}