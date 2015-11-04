/**
 * <pre>
 * UniCast a series of items between 1 publisher and 1 event processor.
 *
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *
 * Disruptor:
 * ==========
 *              track to prevent wrap
 *              +------------------+
 *              |                  |
 *              |                  v
 * +----+    +====+    +====+   +-----+
 * | P1 |--->| RB |<---| SB |   | EP1 |
 * +----+    +====+    +====+   +-----+
 *      claim      get    ^        |
 *                        |        |
 *                        +--------+
 *                          waitFor
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB  - SequenceBarrier
 * EP1 - EventProcessor 1
 *
 * </pre>
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using disruptorpretest.Support;
using disruptorpretest.Support.V3._3._0;

namespace disruptorpretest.V3._3._0.Sequenced
{
    public class OneToOneSequencedLongArrayThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024 * 1;
        private static readonly long ITERATIONS = 1000L * 1000L * 1L;
        private static readonly int ARRAY_SIZE = 2 * 1024;
        private readonly LongArrayEventHandler handler = new LongArrayEventHandler();
        private readonly RingBuffer<long[]> ringBuffer =
        RingBuffer<long[]>.CreateSingleProducer(() => new long[ARRAY_SIZE]
        , BUFFER_SIZE
        , new YieldingWaitStrategy());
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly BatchEventProcessor<long[]> batchEventProcessor;
        public OneToOneSequencedLongArrayThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads (1,1);
            sequenceBarrier = ringBuffer.NewBarrier();
            batchEventProcessor = new BatchEventProcessor<long[]>(ringBuffer, sequenceBarrier, handler);
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            long expectedCount = batchEventProcessor.Sequence.Value + ITERATIONS;
            handler.Reset(latch, ITERATIONS);
            Task.Factory.StartNew(() => batchEventProcessor.Run());
            Stopwatch start = Stopwatch.StartNew();

            RingBuffer<long[]> rb = ringBuffer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = rb.Next();
                long[] @event = rb.Get(next);
                for (int j = 0; j < @event.Length; j++)
                {
                    @event[j] = i;
                }
                rb.Publish(next);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * ARRAY_SIZE * 1000L) / (start.ElapsedMilliseconds);
            waitForEventProcessorSequence(expectedCount);
            batchEventProcessor.Halt();

            PerfTestUtil.failIf(0, handler.Value);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
        private void waitForEventProcessorSequence(long expectedCount)
        {
            while (batchEventProcessor.Sequence.Value != expectedCount)
            {
                Thread.Sleep(1);
            }
        }

    }
}
