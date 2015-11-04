/**
 * <pre>
 *
 * Sequence a series of events from multiple publishers going to one event processor.
 *
 * +----+
 * | P1 |------+
 * +----+      |
 *             v
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *             ^
 * +----+      |
 * | P3 |------+
 * +----+
 *
 * Disruptor:
 * ==========
 *             track to prevent wrap
 *             +--------------------+
 *             |                    |
 *             |                    v
 * +----+    +====+    +====+    +-----+
 * | P1 |--->| RB |<---| SB |    | EP1 |
 * +----+    +====+    +====+    +-----+
 *             ^   get    ^         |
 * +----+      |          |         |
 * | P2 |------+          +---------+
 * +----+      |            waitFor
 *             |
 * +----+      |
 * | P3 |------+
 * +----+
 *
 * P1  - Publisher 1
 * P2  - Publisher 2
 * P3  - Publisher 3
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
using ValueAdditionEventHandler = disruptorpretest.Support.V3._3._0.ValueAdditionEventHandler;

namespace disruptorpretest.V3._3._0.Sequenced
{
    public class ThreeToOneSequencedBatchThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_PUBLISHERS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;

        private readonly Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);
        private readonly ValueBatchPublisher[] valuePublishers = new ValueBatchPublisher[NUM_PUBLISHERS];
        private readonly RingBuffer<ValueEvent> ringBuffer =
        RingBuffer<ValueEvent>.CreateMultiProducer(() => new ValueEvent(), BUFFER_SIZE, new BusySpinWaitStrategy());
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly BatchEventProcessor<ValueEvent> batchEventProcessor;
        private readonly ValueAdditionEventHandler handler = new ValueAdditionEventHandler();

        public ThreeToOneSequencedBatchThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads (4,4);
            sequenceBarrier = ringBuffer.NewBarrier();
            batchEventProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handler);

            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                valuePublishers[i] = new ValueBatchPublisher(cyclicBarrier, ringBuffer, ITERATIONS / NUM_PUBLISHERS, 10);
            }
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            handler.reset(latch, batchEventProcessor.Sequence.Value + ((ITERATIONS / NUM_PUBLISHERS) * NUM_PUBLISHERS));
            Task[] task = new Task[NUM_PUBLISHERS];
            //for (int i = 0; i < NUM_PUBLISHERS; Interlocked.Increment(ref i))
            //{
            //    task[i] = Task.Run(() => valuePublishers[i].Run());
            //}
            task[0] = Task.Factory.StartNew(() => valuePublishers[0].Run());
            task[1] = Task.Factory.StartNew(() => valuePublishers[1].Run());
            task[2] = Task.Factory.StartNew(() => valuePublishers[2].Run());
            //ThreadPool.QueueUserWorkItem(o=>{},null);
            Task.Factory.StartNew(() => batchEventProcessor.Run());
            var stopWatch = Stopwatch.StartNew();
            cyclicBarrier.SignalAndWait();
            try
            { 
                Task.WaitAll(task);
            }
            catch (AggregateException ex) 
            {
                Console.WriteLine(ex.ToString ());
            }
            latch.Wait();

            stopWatch.Stop ();
            var millisecongd=stopWatch.ElapsedMilliseconds;
            long opsPerSecond = (ITERATIONS * 1000L) / (millisecongd==0?1:millisecongd);
            batchEventProcessor.Halt();
            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }

    }
}
