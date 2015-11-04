using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Disruptor;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Disruptor.Scheduler;
using disruptorpretest;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Disruptor_V3_2_0_Reactive._1P1C
{
    public class OnePublisherToOneProcessorRawThroughputTest  : AbstractPerfTestQueueVsDisruptor
    {
        //private int BUFFER_SIZE = 1024 * 64;
        //private long ITERATIONS = 1000L * 1000L * 100L; //1 亿次
        private ISequencer sequencer;
        private MyRunnable myRunnable;

        public OnePublisherToOneProcessorRawThroughputTest()
            : base(TestName, 1000L * 1000L * 100L)
        {
            sequencer = new SingleProducerSequencer(BUFFER_SIZE, new YieldingWaitStrategy());
            myRunnable = new MyRunnable(sequencer);
            sequencer.AddGatingSequences(myRunnable.GetSequence);
        }


        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            var ob = Observable.Create<long>(o =>
            {
                var cancel = new CancellationDisposable(); // internally creates a new CancellationTokenSource
               
                Scheduler.Default.Schedule(() =>
                {
                    for (long i = 0; i < ITERATIONS; i++)
                    {
                        //Thread.Sleep(200);  // here we do the long lasting background operation
                        if (!cancel.Token.IsCancellationRequested)    // check cancel token periodically
                            o.OnNext(i++);
                        else
                        {
                            Console.WriteLine("Aborting because cancel event was signaled!");
                            o.OnCompleted();
                            return;
                        }
                    }
                }
                );

                return cancel;
            }
             );

            var spinwait = default(SpinWait);
            //var latch = new CountdownEvent(1);
            var latch = new ManualResetEvent(false);
            var expectedCount = myRunnable.GetSequence.Value + ITERATIONS;
            myRunnable.Reset(latch, expectedCount);
            var _scheduler = new RoundRobinThreadAffinedTaskScheduler(4);
            //TaskScheduler.Default调度器 CPU 约在 50 % 两个繁忙，两个空闲
            //
           
            Task.Factory.StartNew(myRunnable.Run, CancellationToken.None, TaskCreationOptions.None,TestTaskScheduler);

            var start = Stopwatch.StartNew();

            var sequencer = this.sequencer;
            //var range = Observable.Range(0, 1000 * 1000 * 100, Scheduler.Default)
            //       .Subscribe(i => {
            //           long next = sequencer.Next();
            //           sequencer.Publish(next);
            //       });
           // var subscription = ob.Subscribe(i =>
            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = sequencer.Next();
                sequencer.Publish(next);
            }
           //);

            latch.WaitOne();
            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;

            WaitForEventProcessorSequence(expectedCount, spinwait,myRunnable);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }       
    }
}
