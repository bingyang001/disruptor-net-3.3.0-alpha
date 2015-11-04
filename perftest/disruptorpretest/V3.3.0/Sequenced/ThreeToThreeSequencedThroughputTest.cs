/**
 * <pre>
 *
 * Sequence a series of events from multiple publishers going to one event processor.
 *
 * Disruptor:
 * ==========
 *             track to prevent wrap
 *             +--------------------+
 *             |                    |
 *             |                    |
 * +----+    +====+    +====+       |
 * | P1 |--->| RB |--->| SB |--+    |
 * +----+    +====+    +====+  |    |
 *                             |    v
 * +----+    +====+    +====+  | +----+
 * | P2 |--->| RB |--->| SB |--+>| EP |
 * +----+    +====+    +====+  | +----+
 *                             |
 * +----+    +====+    +====+  |
 * | P3 |--->| RB |--->| SB |--+
 * +----+    +====+    +====+
 *
 * P1 - Publisher 1
 * P2 - Publisher 2
 * P3 - Publisher 3
 * RB - RingBuffer
 * SB - SequenceBarrier
 * EP - EventProcessor
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
using disruptorpretest.Support.V3._3._0;

namespace disruptorpretest.V3._3._0.Sequenced
{
    public class ThreeToThreeSequencedThroughputTest 
    : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_PUBLISHERS = 3;
        private static readonly int ARRAY_SIZE = 3;
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 180L;
        private readonly Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);

        private readonly RingBuffer<long[]>[] buffers = new RingBuffer<long[]>[NUM_PUBLISHERS];
        private readonly ISequenceBarrier[] barriers = new ISequenceBarrier[NUM_PUBLISHERS];
        private readonly LongArrayPublisher[] valuePublishers = new LongArrayPublisher[NUM_PUBLISHERS];
        private readonly LongArrayEventHandler handler = new LongArrayEventHandler();
        private readonly MultiBufferBatchEventProcessor<long[]> batchEventProcessor;

        public ThreeToThreeSequencedThroughputTest() 
        :base(Test_Disruptor,ITERATIONS)
        {
            ThreadPool.SetMaxThreads (4,8);
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                buffers[i] = RingBuffer<long[]>.CreateSingleProducer(
                ()=>new long[ARRAY_SIZE]
                , BUFFER_SIZE
                , new YieldingWaitStrategy());
                barriers[i] = buffers[i].NewBarrier();
                valuePublishers[i] = new LongArrayPublisher(cyclicBarrier,
                                                       buffers[i],
                                                       ITERATIONS / NUM_PUBLISHERS,
                                                       ARRAY_SIZE);  
            }
            batchEventProcessor = new MultiBufferBatchEventProcessor<long[]>(buffers, barriers, handler);
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                buffers[i].AddGatingSequences(batchEventProcessor.getSequences()[i]);
            }
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            handler.Reset(latch, ITERATIONS);
            Task[] task = new Task[NUM_PUBLISHERS];
            //for (int i = 0; i < NUM_PUBLISHERS; i++)
            //{
            //    task[i] = Task.Run(() => valuePublishers[i].run());
            //}
            task[0] = Task.Factory.StartNew(() => valuePublishers[0].run());
            task[1] = Task.Factory.StartNew(() => valuePublishers[1].run());
            task[2] = Task.Factory.StartNew(() => valuePublishers[2].run());

            ThreadPool.QueueUserWorkItem(o=>
            {
                batchEventProcessor.Run ();
            });
            var stopWatch = Stopwatch.StartNew();
            cyclicBarrier.SignalAndWait();

            Task.WaitAll(task);
            latch.Wait();

            long opsPerSecond = (ITERATIONS * 1000L) / (stopWatch.ElapsedMilliseconds);
            batchEventProcessor.Halt();
            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
