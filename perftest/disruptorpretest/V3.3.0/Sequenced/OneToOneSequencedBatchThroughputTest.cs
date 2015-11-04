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
using System.Threading.Tasks.Schedulers;
using Disruptor;
using disruptorpretest.Support;
using ValueAdditionEventHandler = disruptorpretest.Support.V3._3._0.ValueAdditionEventHandler;
namespace disruptorpretest.V3._3._0.Sequenced
{
    public class OneToOneSequencedBatchThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        public static readonly int BATCH_SIZE = 10;
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;

        private readonly RingBuffer<ValueEvent> ringBuffer =
           RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent()
                                                       , BUFFER_SIZE
                                                       , new YieldingWaitStrategy());
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly ValueAdditionEventHandler handler = new ValueAdditionEventHandler();
        private readonly long expectedResult = PerfTestUtil.AccumulatedAddition(ITERATIONS) * BATCH_SIZE;

        private readonly BatchEventProcessor<ValueEvent> batchEventProcessor;
        public OneToOneSequencedBatchThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads (4,4);
            sequenceBarrier = ringBuffer.NewBarrier();
            batchEventProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handler);
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);
        }
        private void waitForEventProcessorSequence(long expectedCount)
        {
            while (batchEventProcessor.Sequence.Value != expectedCount)
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
            //ManualResetEvent latch = new ManualResetEvent(false);
            long expectedCount = batchEventProcessor.Sequence.Value + ITERATIONS * BATCH_SIZE;
            handler.reset(latch, expectedCount);
            Task.Factory.StartNew(() => batchEventProcessor.Run()
                                , CancellationToken.None
                                , TaskCreationOptions.LongRunning
                                , new LimitedConcurrencyLevelTaskScheduler(4));
            //ThreadPool.QueueUserWorkItem(o=>batchEventProcessor.Run());
            Stopwatch start = Stopwatch.StartNew();

            RingBuffer<ValueEvent> rb = ringBuffer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long hi = rb.Next(BATCH_SIZE);
                long lo = hi - (BATCH_SIZE - 1);
                for (long l = lo; l <= hi; l++)
                {
                    rb.Get(l).Value = i;
                }
                rb.Publish(lo, hi);
            }

            latch.Wait();
            long opsPerSecond = (BATCH_SIZE * ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            waitForEventProcessorSequence(expectedCount);
            batchEventProcessor.Halt();

            Console.WriteLine("expectedResult={0:###,###,###},realValue={1:###,###,###}", expectedResult, handler.Value);
            PerfTestUtil.failIfNot(expectedResult, handler.Value);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
