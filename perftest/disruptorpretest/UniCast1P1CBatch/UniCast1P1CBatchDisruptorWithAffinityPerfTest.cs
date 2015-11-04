using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using disruptorpretest.Support;
using Disruptor.Scheduler;
using NUnit.Framework;
using Disruptor;

namespace disruptorpretest.UniCast1P1CBatch
{
    [TestFixture]
    public class UniCast1P1CBatchDisruptorWithAffinityPerfTest:AbstractUniCast1P1CBatchPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;
        private readonly Disruptor<ValueEvent> _disruptor;
        private readonly ManualResetEvent _mru;
        private readonly RoundRobinThreadAffinedTaskScheduler _scheduler;

        public UniCast1P1CBatchDisruptorWithAffinityPerfTest()
            : base(100 * Million)
        {
            _scheduler = new RoundRobinThreadAffinedTaskScheduler(2);

            _disruptor = new Disruptor<ValueEvent>(() => new ValueEvent(), BufferSize, _scheduler, ProducerType.SINGLE, new YieldingWaitStrategy());
            _mru = new ManualResetEvent(false);
            _eventHandler = new ValueAdditionEventHandler(Iterations, _mru);
            _disruptor.handleEventsWith(_eventHandler);
            _ringBuffer = _disruptor.getRingBuffer();
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }

        public override long RunPass()
        {
            _disruptor.start();

            const int batchSize = 10;
            var batchDescriptor = _ringBuffer.NewBatchDescriptor(batchSize);

            var sw = Stopwatch.StartNew();

            Task.Factory.StartNew(
                () =>
                    {
                        long offset = 0;
                        for (long i = 0; i < Iterations; i += batchSize)
                        {
                            _ringBuffer.next(batchDescriptor);
                            for (long sequence = batchDescriptor.Start; sequence <= batchDescriptor.End; sequence++)
                            {
                                _ringBuffer[sequence].Value = offset++;
                            }
                            _ringBuffer.publish(batchDescriptor);
                        }
                    }, CancellationToken.None, TaskCreationOptions.None, _scheduler);

            _mru.WaitOne();

            long opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _disruptor.Shutdown();
            _scheduler.Dispose();

            Assert.AreEqual(ExpectedResult, _eventHandler.Value);

            return opsPerSecond;
        }
    }
}