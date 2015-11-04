/**
 * <pre>
 * UniCast a series of items between 1 publisher and 1 event processor.
 *
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *
 *
 * Queue Based:
 * ============
 *
 *        put      take
 * +----+    +====+    +-----+
 * | P1 |--->| Q1 |<---| EP1 |
 * +----+    +====+    +-----+
 *
 * P1  - Publisher 1
 * Q1  - Queue 1
 * EP1 - EventProcessor 1
 *
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

namespace disruptorpretest.V3._3._0.Raw
{
    public class OneToOneRawThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 200L;
        private readonly ISequencer sequencer = new SingleProducerSequencer(BUFFER_SIZE, new YieldingWaitStrategy());
        private readonly _MyRunnable myRunnable;
        public OneToOneRawThroughputTest()
            : base(Test_Disruptor, ITERATIONS)
        {
            myRunnable = new _MyRunnable(this.sequencer);
            this.sequencer.AddGatingSequences(myRunnable.GetSequence);
        }

        private void waitForEventProcessorSequence(long expectedCount)
        {
            while (myRunnable.GetSequence.Value != expectedCount)
            {
                Thread.Sleep(1);
            }
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            long expectedCount = myRunnable.GetSequence.Value + ITERATIONS;
            myRunnable.Reset(latch, expectedCount);
            Task.Factory.StartNew(() => myRunnable.Run());
            Stopwatch start = Stopwatch.StartNew();

            ISequenced sequencer = this.sequencer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = sequencer.Next();
                sequencer.Publish(next);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            waitForEventProcessorSequence(expectedCount);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
