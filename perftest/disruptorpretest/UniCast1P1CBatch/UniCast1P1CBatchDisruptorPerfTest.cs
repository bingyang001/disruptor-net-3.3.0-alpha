using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using disruptorpretest.Support;
using NUnit.Framework;
using Disruptor;

namespace disruptorpretest.UniCast1P1CBatch
{
    [TestFixture]
    public class UniCast1P1CBatchDisruptorPerfTest:AbstractUniCast1P1CBatchPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;
        private readonly Disruptor<ValueEvent> _disruptor;
        private readonly ManualResetEvent _mru;

        public UniCast1P1CBatchDisruptorPerfTest()
            : base(100 * Million)
        {
            _disruptor = new Disruptor<ValueEvent>(()=>new ValueEvent(),BufferSize,TaskScheduler.Default, ProducerType.SINGLE,new YieldingWaitStrategy());
                                                  
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
            var batchDescriptor = _ringBuffer.newBatchDescriptor(batchSize);

            var sw = Stopwatch.StartNew();

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

            _mru.WaitOne();

            long opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _disruptor.Shutdown();

            Assert.AreEqual(ExpectedResult, _eventHandler.Value);

            return opsPerSecond;
        }
    }
}