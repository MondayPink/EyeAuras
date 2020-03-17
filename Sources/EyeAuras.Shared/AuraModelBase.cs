using System;
using System.Reactive.Disposables;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraModelBase : DisposableReactiveObject, IAuraModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraModelBase));

        protected AuraModelBase()
        {
            Disposable.Create(() => Log.Debug($"Disposing AuraModel of type {GetType()}, instance: {this}")).AddTo(Anchors);
        }

        public IAuraProperties Properties
        {
            get => SaveProperties();
            set => LoadProperties(value);
        }

        protected abstract void LoadProperties(IAuraProperties source);

        protected abstract IAuraProperties SaveProperties();
    }

    public abstract class AuraModelBase<T> : AuraModelBase, IAuraModel<T> where T : class, IAuraProperties, new()
    {
        public new T Properties
        {
            get => Save();
            set => Load(value);
        }

        private void Load(T source)
        {
            VisitLoad(source);
        }

        private T Save()
        {
            var result = new T();
            VisitSave(result);
            return result;
        }

        protected abstract void VisitSave(T source);

        protected abstract void VisitLoad(T source);
        
        protected override IAuraProperties SaveProperties()
        {
            var result = Save();
            VisitSave(result);
            return result;
        }

        protected override void LoadProperties(IAuraProperties source)
        {
            if (!(source is T typedSource))
            {
                throw new ArgumentException(
                    $"Invalid Properties source, expected value of type {typeof(T)}, got {(source == null ? "null" : source.GetType().FullName)} instead");
            }

            Load(typedSource);
        }
    }
}