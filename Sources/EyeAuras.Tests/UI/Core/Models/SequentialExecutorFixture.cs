using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using EyeAuras.UI.Core.Models;
using log4net;
using log4net.Core;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using PoeShared;
using PoeShared.Scaffolding;
using Shouldly;
// ReSharper disable UnusedVariable

namespace EyeAuras.Tests.UI.Core.Models
{
    [TestFixture]
    public class ThreadExecutorFixture
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThreadExecutorFixture));
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

        private const string OnEnter = "OnEnter";
        private const string WhileActive = "WhileActive";
        private const string OnExit = "OnExit";
        
        private TestScheduler testScheduler;
        private IScheduler bgScheduler;

        private ConcurrentQueue<string> eventOrder;

        [SetUp]
        public void SetUp()
        {
            SharedLog.Instance.AddConsoleAppender();
            SharedLog.Instance.SwitchLoggingLevel(Level.All);
            testScheduler = new TestScheduler();
            bgScheduler = Scheduler.Default.DisableOptimizations();
            eventOrder = new ConcurrentQueue<string>();
        }

        [Test]
        public void ShouldExecuteOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            SetIsActive(instance, true);

            //When
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter });
        }
        
        [Test]
        public void ShouldNotExecuteOnExitWithoutOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            SetIsActive(instance, false);

            //When
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(false);

            //Then
            eventOrder.ShouldBeEmpty();
        }
        
        [Test]
        [Repeat(10)]
        public void ShouldExecuteOnExitAfterOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            SetIsActive(instance, false);

            //When
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit });
        }
        
        [Test]
        public void ShouldExecuteOnExitAfterOnEnterWhileExecutingOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            onEnterExecution.Reset();
            SetIsActive(instance, true);
            SetIsActive(instance, false);
            
            eventOrder.ShouldBeEmpty();

            //When
            onEnterExecution.Set();
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit });
        }

        [Test]
        public void ShouldNotExecuteOnEnterMultipleTimes()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            eventOrder.ShouldBe(new []{ OnEnter });

            //When
            SetIsActive(instance, true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter });
        }
        
        [Test]
        public void ShouldNotExecuteOnExitMultipleTimes()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);

            onEnterExecution.Reset();
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(false);

            SetIsActive(instance, false);
            SetIsActive(instance, true);
            SetIsActive(instance, false);
            onEnterExecution.Set();
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //When
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit }, Case.Insensitive, $"Transitions: {eventOrder.ToArray().DumpToTextRaw()}");
        }
        
        [Test]
        [Repeat(10)]
        public void ShouldNotExecuteOnEnterWhenAlreadyExecutingOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            onEnterExecution.Reset();
            SetIsActive(instance, true);
            SetIsActive(instance, false);
            SetIsActive(instance, true);

            eventOrder.ShouldBeEmpty();

            //When
            onEnterExecution.Set();
            onEnterCompletion.WaitOne(WaitTimeout);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter });
        }

        [Test]
        public void ShouldNotExecuteOnExitTwiceWhenCompletedOnEnter()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);

            onEnterExecution.Reset();
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(false);
            eventOrder.ShouldBeEmpty();

            SetIsActive(instance, false);
            SetIsActive(instance, true);
            SetIsActive(instance, false);

            onEnterExecution.Set();
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //When
            onExitCompletion.WaitOne(WaitTimeout);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit });
        }
        
        [Test]
        public void ShouldNotExecuteOnExitTwiceWhenExecutingOnExit()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            onEnterExecution.Reset();
            onExitExecution.Reset();
            
            SetIsActive(instance, true);
            SetIsActive(instance, false);
            SetIsActive(instance, true);
            SetIsActive(instance, false);
            
            eventOrder.ShouldBeEmpty();
            onEnterExecution.Set();
            onEnterCompletion.WaitOne(WaitTimeout);
            eventOrder.ShouldBe(new []{ OnEnter });
            
            //When
            onExitExecution.Set();
            onExitCompletion.WaitOne(WaitTimeout);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit });
        }
        
        [Test]
        public void ShouldNotExecuteOnExitInitially()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);

            //When
            SetIsActive(instance, false);
            
            //Then
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(false);
            eventOrder.ShouldBeEmpty();
        }

        [Test]
        [Repeat(10)]
        public void ShouldNotTerminate()
        {
            //Given
            var instance = CreateInstance();
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);

            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            SetIsActive(instance, false);
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            
            eventOrder.ShouldBe(new []{ OnEnter, OnExit });

            onEnterCompletion.Reset();
            onExitCompletion.Reset();

            onEnterExecution.Set();
            onExitExecution.Set();

            //When
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            eventOrder.ShouldBe(new []{ OnEnter, OnExit, OnEnter });
            
            SetIsActive(instance, false);
            onExitCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, OnExit, OnEnter, OnExit });
        }

        [Test]
        public void ShouldExecuteWhileActive()
        {
            //Given
            var instance = CreateInstance();
            instance.WhileActivePeriod = TimeSpan.FromMilliseconds(1);
            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);

            whileActiveExecution.Reset();
            
            SetIsActive(instance, true);
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(true);
            
            eventOrder.ShouldBe(new []{ OnEnter });

            //When
            whileActiveExecution.Set();
            whileActiveCompletion.WaitOne(WaitTimeout).ShouldBe(true);

            //Then
            eventOrder.ShouldBe(new []{ OnEnter, WhileActive });
        }

        [Test]
        public void ShouldNotExecuteAfterDispose()
        {
            //Given
            var instance = CreateInstance();

            SubscribeAll(
                instance, 
                out var onEnterExecution, 
                out var onEnterCompletion, 
                out var whileActiveExecution, 
                out var whileActiveCompletion, 
                out var onExitExecution, 
                out var onExitCompletion);
            
            instance.Dispose();
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(false);

            //When
            SetIsActive(instance, true);

            //Then
            onEnterCompletion.WaitOne(WaitTimeout).ShouldBe(false);
        }

        private void SetIsActive(IThreadExecutor instance, bool value)
        {
            WriteLog($"Sending {(value ? "IsActive" : "IsNotActive")}");
            instance.IsActive = value;
            WriteLog($"{(value ? "IsActive" : "IsNotActive")} sent");
        }

        private void AdvanceBackground()
        {
            WriteLog($"Advancing background scheduler");
            testScheduler.AdvanceBy(1);
            WriteLog($"Advanced background scheduler");
        }

        private void SubscribeAll(
            IThreadExecutor instance, 
            out AutoResetEvent onEnterExecution, 
            out AutoResetEvent onEnterCompletion, 
            out AutoResetEvent whileActiveExecution, 
            out AutoResetEvent whileActiveCompletion, 
            out AutoResetEvent onExitExecution,
            out AutoResetEvent onExitCompletion)
        {
            SubscribeToSink(OnEnter, eventOrder, instance.WhenActivated, out onEnterExecution, out onEnterCompletion);
            SubscribeToSink(OnExit, eventOrder, instance.WhenDeactivated, out onExitExecution, out onExitCompletion);
            SubscribeToSink(WhileActive, eventOrder, instance.WhileActive, out whileActiveExecution, out whileActiveCompletion);
        }
        
        private static void SubscribeToSink(
            string sinkName,
            ConcurrentQueue<string> eventOrder,
            IObservable<Unit> sink, 
            out AutoResetEvent executionSignal,
            out AutoResetEvent completionSignal)
        {
            var execution = new AutoResetEvent(true);
            var completion = new AutoResetEvent(false);
            sink.Subscribe(_ =>
            {
                WriteLog($"-> {sinkName}");
                execution.WaitOne();
                eventOrder.Enqueue(sinkName);
                completion.Set();
                WriteLog($"<- {sinkName}");
            });
            completionSignal = completion;
            executionSignal = execution;
        }
        
        private static void WriteLog(string message)
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
        
        private IThreadExecutor CreateInstance()
        {
            return new ThreadExecutor(bgScheduler);
        }
    }
}