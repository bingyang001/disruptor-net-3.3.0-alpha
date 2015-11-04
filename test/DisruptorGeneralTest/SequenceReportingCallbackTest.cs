using System.Threading;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class SequenceReportingCallbackTest
    {
        private readonly ManualResetEvent _callbackLatch = new ManualResetEvent(false);
        private readonly ManualResetEvent _onEndOfBatchLatch = new ManualResetEvent(false);

        [Test]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            var ringBuffer =  RingBuffer<StubEvent>.CreateMultiProducer(() => new StubEvent(0), 16);
            var sequenceBarrier = ringBuffer.NewBarrier();
            var handler = new TestSequenceReportingEventHandler(_callbackLatch);
            var batchEventProcessor = new BatchEventProcessor<StubEvent>(ringBuffer, sequenceBarrier, handler);
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);

            var thread = new Thread(batchEventProcessor.Run) { IsBackground = true };
            thread.Start();

            Assert.AreEqual(-1L, batchEventProcessor.Sequence.Value);
            ringBuffer.Publish(ringBuffer.Next());

            _callbackLatch.WaitOne();
            Assert.AreEqual(0L, batchEventProcessor.Sequence.Value);

            _onEndOfBatchLatch.Set();
            Assert.AreEqual(0L, batchEventProcessor.Sequence.Value);

            batchEventProcessor.Halt();
            thread.Join();
        }

        private class TestSequenceReportingEventHandler : ISequenceReportingEventHandler<StubEvent>
        {
            private Sequence _sequenceCallback;
            private readonly ManualResetEvent _callbackLatch;

            public TestSequenceReportingEventHandler(ManualResetEvent callbackLatch)
            {
                _callbackLatch = callbackLatch;
            }           

            #region ISequenceReportingEventHandler<StubEvent> 成员

            public void SetSequenceCallback(Sequence sequenceCallback)
            {
                _sequenceCallback = sequenceCallback;
            }

            #endregion

            #region IEventHandler<StubEvent> 成员

            public void OnEvent(StubEvent @event, long sequence, bool endOfBatch)
            {
                _sequenceCallback.Value = sequence;
                _callbackLatch.Set();
            }

            #endregion
        }
    }
}
