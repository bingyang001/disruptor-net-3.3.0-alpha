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
    public class OneToOneSequencedPollerThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;
        private readonly long expectedResult = PerfTestUtil.AccumulatedAddition(ITERATIONS);

        private readonly RingBuffer<ValueEvent> ringBuffer =
        RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent()
        , BUFFER_SIZE
        , new YieldingWaitStrategy());

        private readonly EventPoller<ValueEvent> poller;
        private readonly PollRunnable pollRunnable;

        public OneToOneSequencedPollerThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads (1,1);
            poller = ringBuffer.NewPoller();
            pollRunnable = new PollRunnable(poller);
            ringBuffer.AddGatingSequences(poller.GetSequence());  
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            long expectedCount = poller.GetSequence().Value + ITERATIONS;
            pollRunnable.reset(latch, expectedCount);
            Task.Factory.StartNew(() => pollRunnable.Run());
            Stopwatch watch = Stopwatch.StartNew();

            RingBuffer<ValueEvent> rb = ringBuffer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = rb.Next();
                rb[next].Value = i;
                rb.Publish(next);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (watch.ElapsedMilliseconds);
            waitForEventProcessorSequence(expectedCount);
            pollRunnable.Halt();

            PerfTestUtil.failIfNot(expectedResult, pollRunnable.getValue());

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
        private void waitForEventProcessorSequence(long expectedCount)
        {
            while (poller.GetSequence().Value != expectedCount)
            {
                Thread.Sleep(1);
            }
        }
    }
}
