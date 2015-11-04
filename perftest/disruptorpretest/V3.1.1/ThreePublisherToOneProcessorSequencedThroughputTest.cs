using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Disruptor;
using disruptorpretest.Support;
using System.Threading.Tasks;

namespace disruptorpretest.V3._1._1
{

    /// <summary>
    /// 
    /// <pre>
    ///
    /// Sequence a series of events from multiple publishers going to one event processor.
    ///
    /// +----+
    /// | P1 |------+
    /// +----+      |
    ///             v
    /// +----+    +-----+
    /// | P1 |--->| EP1 |
    /// +----+    +-----+
    ///             ^
    /// +----+      |
    /// | P3 |------+
    /// +----+
    ///
    ///
    /// Queue Based:
    /// ============
    ///
    /// +----+  put
    /// | P1 |------+
    /// +----+      |
    ///             v   take
    /// +----+    +====+    +-----+
    /// | P2 |--->| Q1 |<---| EP1 |
    /// +----+    +====+    +-----+
    ///             ^
    /// +----+      |
    /// | P3 |------+
    /// +----+
    ///
    /// P1  - Publisher 1
    /// P2  - Publisher 2
    /// P3  - Publisher 3
    /// Q1  - Queue 1
    /// EP1 - EventProcessor 1
    ///
    ///
    /// Disruptor:
    /// ==========
    ///             track to prevent wrap
    ///             +--------------------+
    ///             |                    |
    ///             |                    v
    /// +----+    +====+    +====+    +-----+
    /// | P1 |--->| RB |<---| SB |    | EP1 |
    /// +----+    +====+    +====+    +-----+
    ///             ^   get    ^         |
    /// +----+      |          |         |
    /// | P2 |------+          +---------+
    /// +----+      |            waitFor
    ///             |
    /// +----+      |
    /// | P3 |------+
    /// +----+
    ///
    /// P1  - Publisher 1
    /// P2  - Publisher 2
    /// P3  - Publisher 3
    /// RB  - RingBuffer
    /// SB  - SequenceBarrier
    /// EP1 - EventProcessor 1
    ///
    /// </pre>
    ////
    /// </summary>
    public class ThreePublisherToOneProcessorSequencedThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_PUBLISHERS = 3;
        private const int BUFFER_SIZE = 1024 * 64;
        private const long ITERATIONS = 1000L * 1000L * 20L;
        private Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);
        private RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateMultiProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private ISequenceBarrier sequenceBarrier;
        private ValueAdditionEventHandler_V3 handler = new ValueAdditionEventHandler_V3();
        private BatchEventProcessor<ValueEvent> batchEventProcessor;
        private ValuePublisher_V3[] valuePublishers = new ValuePublisher_V3[NUM_PUBLISHERS];

        public ThreePublisherToOneProcessorSequencedThroughputTest()
            : base(TestName, ITERATIONS)
        {
            sequenceBarrier = ringBuffer.NewBarrier();
            batchEventProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handler);
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                valuePublishers[i] = new ValuePublisher_V3(cyclicBarrier, ringBuffer, ITERATIONS / NUM_PUBLISHERS);
            }

            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            var latch = new ManualResetEvent(false);
            handler.Reset(latch, batchEventProcessor.Sequence.Value + ((ITERATIONS / NUM_PUBLISHERS) * NUM_PUBLISHERS));
            var list = new List<TaskFactory>();


            foreach (var item in valuePublishers)
            {
                Task.Factory.StartNew(() => item.Run());
            }
            Task.Factory.StartNew(() => batchEventProcessor.Run());

            var runTime = System.Diagnostics.Stopwatch.StartNew();

            cyclicBarrier.SignalAndWait();

            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                // futures[i].get();
            }

            latch.WaitOne();

            long opsPerSecond = (ITERATIONS * 1000L) / runTime.ElapsedMilliseconds;
            batchEventProcessor.Halt();

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
