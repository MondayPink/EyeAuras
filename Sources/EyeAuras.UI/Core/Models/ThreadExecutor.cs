using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;
using Stateless;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class ThreadExecutor : DisposableReactiveObject, IThreadExecutor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThreadExecutor));

        private readonly ISubject<Unit> whenActivated = new Subject<Unit>();
        private readonly ISubject<Unit> whenDeactivated = new Subject<Unit>();
        private readonly ISubject<Unit> whileActive = new Subject<Unit>();
        private TimeSpan whileActivePeriod;
        private bool isActive;
        
        private readonly ConcurrentQueue<Trigger> triggerQueue = new ConcurrentQueue<Trigger>();
        private readonly StateMachine<State, Trigger> stateMachine = new StateMachine<State, Trigger>(State.Idle);
        private readonly SerialDisposable whileActiveTimerAnchor = new SerialDisposable();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        public ThreadExecutor(IScheduler bgScheduler)
        {
            stateMachine.OnTransitioned(x =>
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Transition: {x.Source} => {x.Destination} via {x.Trigger}");
                }
            });
            stateMachine
                .Configure(State.Idle)
                .PermitReentry(Trigger.Initialize)
                .Ignore(Trigger.WhileActive)
                .Ignore(Trigger.Deactivation)
                .Permit(Trigger.Activation, State.IsActive);
            
            stateMachine
                .Configure(State.IsActive)
                .Ignore(Trigger.Activation)
                .PermitReentry(Trigger.WhileActive)
                .Permit(Trigger.Deactivation, State.IsNotActive);

            stateMachine
                .Configure(State.IsNotActive)
                .Permit(Trigger.Initialize, State.Idle)
                .Ignore(Trigger.Deactivation);

            stateMachine
                .Configure(State.IsActive)
                .OnEntryFrom(Trigger.Activation, x =>
                {
                    whenActivated.OnNext(Unit.Default);
                    whileActiveTimerAnchor.Disposable =
                        this.WhenAnyValue(x => x.WhileActivePeriod)
                            .Select(x => x <= TimeSpan.Zero ? Observable.Empty<long>() : Observable.Timer(DateTimeOffset.Now, x, bgScheduler))
                            .Switch()
                            .Subscribe(() =>
                            {
                                if (triggerQueue.TryPeek(out var lastTrigger) && lastTrigger == Trigger.WhileActive)
                                {
                                    return;
                                }
                                triggerQueue.Enqueue(Trigger.WhileActive);
                            });
                })
                .OnEntryFrom(Trigger.WhileActive, () =>
                {
                    whileActive.OnNext(Unit.Default);
                });

            stateMachine
                .Configure(State.IsNotActive)
                .OnEntry(x =>
                {
                    whileActiveTimerAnchor.Disposable = null;
                    whenDeactivated.OnNext(Unit.Default);
                    stateMachine.Fire(Trigger.Initialize);
                });
            
            this.WhenAnyValue(x => x.IsActive)
                .WithPrevious((prev, curr) => new { prev, curr })
                .Where(x => x.prev != x.curr)
                .Select(x =>
                {
                    if (x.prev == false && x.curr == true)
                    {
                        return Trigger.Activation;
                    } else if (x.prev == true && x.curr == false)
                    {
                        return Trigger.Deactivation;
                    }
                    else
                    {
                        throw new NotSupportedException($"This state is not supported: {x}");
                    }
                })
                .Subscribe(trigger =>
                {
                    triggerQueue.Enqueue(trigger);
                }, Log.HandleException)
                .AddTo(Anchors);
            
            var task = new Task(StateMachineLoop, cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            Disposable.Create(() =>
            {
                try
                {
                    Log.Debug($"Cancelling processing thread");
                    cancellationTokenSource.Cancel();
                    try
                    {
                        Log.Debug($"Awaiting processing thread termination");
                        task.Wait();
                        Log.Debug($"Processing thread terminated");
                    }
                    finally
                    {
                        Log.Debug($"Disposing cancellation token source");
                        cancellationTokenSource.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to cancel task", ex);
                }
            }).AddTo(Anchors);
            
            task.Start();

            Disposable.Create(() => Log.Debug($"ThreadExecutor disposed successfully")).AddTo(Anchors);
        }

        public bool IsActive
        {
            get => isActive;
            set => RaiseAndSetIfChanged(ref isActive, value);
        }

        public TimeSpan WhileActivePeriod
        {    
            get => whileActivePeriod;
            set => RaiseAndSetIfChanged(ref whileActivePeriod, value);
        }

        public IObservable<Unit> WhenActivated => whenActivated;

        public IObservable<Unit> WhileActive => whileActive;

        public IObservable<Unit> WhenDeactivated => whenDeactivated;

        private void StateMachineLoop([CanBeNull] object args)
        {
            
            try
            {
                if (!(args is CancellationToken cancellationToken))
                {
                    return;
                }

                var itemsToExecute = new List<Trigger>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    itemsToExecute.Clear();
                    while (triggerQueue.TryDequeue(out var item))
                    {
                        itemsToExecute.Add(item);
                        if (triggerQueue.Count == 0)
                        {
                            break;
                        }
                    }

                    if (!itemsToExecute.Any())
                    {
                        continue;
                    }

                    if (Log.IsDebugEnabled && itemsToExecute.Count > 1)
                    {
                        Log.Debug($"Items to execute in {stateMachine.State}: {string.Join(", ", itemsToExecute)}");
                    }
                    var itemToExecute = itemsToExecute.Aggregate(Trigger.Initialize, (result, item) =>
                    {
                        if (result != default && result != Trigger.WhileActive && item == Trigger.WhileActive)
                        {
                            return result;
                        }
                        return item;
                    });
                    
                    stateMachine.Fire(itemToExecute);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug($"ThreadExecutor processing thread gracefully terminated");
            }
            catch (Exception e)
            {
                Log.Error($"Error in ThreadExecutor processing thread", e);
                throw;
            }
            finally
            {
                Log.Debug($"ThreadExecutor processing thread completed");
            }
        }

        private enum Trigger
        {
            Initialize,
            Activation,
            WhileActive,
            Deactivation,
        }

        private enum State
        {
            Idle,
            IsActive,
            IsNotActive,
        }
    }
}