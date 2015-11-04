using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;

using NUnit.Framework;
using Disruptor;
using disruptorpretest.Support;
using System;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Disruptor_V3_2_0_Reactive.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CDisruptorPerfTest : AbstractDiamondPath1P3CPerfTest
    {
        private readonly RingBuffer<FizzBuzzEvent> _ringBuffer;
        private readonly FizzBuzzEventHandler _fizzEventHandler;
        private readonly FizzBuzzEventHandler _buzzEventHandler;
        private readonly FizzBuzzEventHandler _fizzBuzzEventHandler;
        private readonly ManualResetEvent _mru;
        private readonly Disruptor<FizzBuzzEvent> _disruptor;


        public DiamondPath1P3CDisruptorPerfTest()
            : base(100 * Million)
        {
            _disruptor = new Disruptor<FizzBuzzEvent>(() => new FizzBuzzEvent(), 1024 * 8,
                                                      TaskScheduler.Default, ProducerType.SINGLE, new BlockingWaitStrategy());
            _mru = new ManualResetEvent(false);
            _fizzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Fizz, Iterations, _mru);
            _buzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Buzz, Iterations, _mru);
            _fizzBuzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.FizzBuzz, Iterations, _mru);

            _disruptor.HandleEventsWith(_fizzEventHandler, _buzzEventHandler)
                      .Then(_fizzBuzzEventHandler);
            _ringBuffer = _disruptor.GetRingBuffer;
        }

        public override long RunPass()
        {
            var ob = Observable.Create<long>(o =>
             {
                 var cancel = new CancellationDisposable(); // internally creates a new CancellationTokenSource
                 Scheduler.Default.Schedule(() =>
                 {
                     for (long i = 0; i < Iterations; i++)
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

            _disruptor.Start();

            var sw = Stopwatch.StartNew();
            var subscription = ob.Subscribe(i =>
            {
                
                var sequence = _ringBuffer.Next();
                _ringBuffer[sequence].Value = i;
                _ringBuffer.Publish(sequence);
            });
            //这里取消任务
            //subscription.Dispose();
            //for (long i = 0; i < Iterations; i++)
            //{
            //    var sequence = _ringBuffer.Next();
            //    _ringBuffer[sequence].Value = i;
            //    _ringBuffer.Publish(sequence);
            //}

            _mru.WaitOne();

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _disruptor.Shutdown();

            Assert.AreEqual(ExpectedResult, _fizzBuzzEventHandler.FizzBuzzCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}