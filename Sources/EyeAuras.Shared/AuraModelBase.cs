using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraModelBase : DisposableReactiveObject, IAuraModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraModelBase));
        
        private static readonly ConcurrentDictionary<Type, HashSet<string>> PropertiesToTrackByType = new ConcurrentDictionary<Type, HashSet<string>>();
        
        private IAuraContext context;

        protected AuraModelBase()
        {
            BuildPropertiesChangeDetector()
                .Subscribe(x =>
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"[{this}] Raising {nameof(Properties)} due to {x} change");
                    }
                    RaisePropertyChanged(nameof(Properties));
                }).AddTo(Anchors);
            Disposable.Create(() => Log.Debug($"Disposing AuraModel of type {GetType()}, instance: {this}")).AddTo(Anchors);
        }

        public IAuraProperties Properties
        {
            get => SaveProperties();
            set => LoadProperties(value);
        }

        public IAuraContext Context
        {
            get => context;
            set => RaiseAndSetIfChanged(ref context, value);
        }

        protected void RaisePropertiesWhen<T>(IObservable<T> source)
        {
            source
                .Subscribe(() => RaisePropertyChanged(nameof(Properties))).AddTo(Anchors);
        }
        
        protected void RaisePropertiesWhen<T>(params IObservable<T>[] sources)
        {
            Observable
                .Merge(sources)
                .Subscribe(() => RaisePropertyChanged(nameof(Properties))).AddTo(Anchors);
        }

        private IObservable<string> BuildPropertiesChangeDetector()
        {
            var properties = PropertiesToTrackByType.GetOrAdd(this.GetType(), _ => new HashSet<string>(BuildPropertiesToTrack()));
            return properties.Count == 0 ? Observable.Empty<string>() : this.WhenAnyProperty()
                .Where(x => properties.Contains( x.EventArgs.PropertyName))
                .Select(x => x.EventArgs.PropertyName);
        }

        private IEnumerable<string> BuildPropertiesToTrack()
        {
            var publicProperties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttribute<AuraPropertyAttribute>() != null)
                .Select(x => x.Name)
                .ToArray();
            return publicProperties;
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

        protected abstract void VisitSave(T properties);

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