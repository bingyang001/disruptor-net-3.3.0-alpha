using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using disruptorpretest.Support;
using NUnit.Framework;
using Disruptor;
using Disruptor.Dsl;

namespace disruptorpretest.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CDisruptorPerfTest : AbstractUniCast1P1CPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;
        private readonly Disruptor<ValueEvent> _disruptor;
        private readonly ManualResetEvent _mru;

        public UniCast1P1CDisruptorPerfTest()
            : base(100 * Million)
        {
            _disruptor = new Disruptor<ValueEvent>(() => new ValueEvent(),BufferSize,TaskScheduler.Default, ProducerType.SINGLE,new YieldingWaitStrategy());                                                         

            _mru = new ManualResetEvent(false);
            _eventHandler = new ValueAdditionEventHandler(Iterations, _mru);
            _disruptor.HandleEventsWith(_eventHandler);
            _ringBuffer = _disruptor.GetRingBuffer;
        }

        public override long RunPass()
        {
            _disruptor.Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                long sequence = _ringBuffer.Next();
                _ringBuffer[sequence].Value = i;
                _ringBuffer.Publish(sequence);
            }

            _mru.WaitOne();

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _disruptor.Shutdown();

            Assert.AreEqual(ExpectedResult, _eventHandler.Value, "RunDisruptorPass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}