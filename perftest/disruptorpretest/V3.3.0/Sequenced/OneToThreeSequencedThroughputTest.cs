
/**
 * <pre>
 *
 * MultiCast a series of items between 1 publisher and 3 event processors.
 *
 *           +-----+
 *    +----->| EP1 |
 *    |      +-----+
 *    |
 * +----+    +-----+
 * | P1 |--->| EP2 |
 * +----+    +-----+
 *    |
 *    |      +-----+
 *    +----->| EP3 |
 *           +-----+
 *
 * Disruptor:
 * ==========
 *                             track to prevent wrap
 *             +--------------------+----------+----------+
 *             |                    |          |          |
 *             |                    v          v          v
 * +----+    +====+    +====+    +-----+    +-----+    +-----+
 * | P1 |--->| RB |<---| SB |    | EP1 |    | EP2 |    | EP3 |
 * +----+    +====+    +====+    +-----+    +-----+    +-----+
 *      claim      get    ^         |          |          |
 *                        |         |          |          |
 *                        +---------+----------+----------+
 *                                      waitFor
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB  - SequenceBarrier
 * EP1 - EventProcessor 1
 * EP2 - EventProcessor 2
 * EP3 - EventProcessor 3
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

namespace disruptorpretest.V3._3._0.Sequenced
{
//测试不过
    public class OneToThreeSequencedThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;

        private readonly long[] results = new long[NUM_EVENT_PROCESSORS];
        private readonly RingBuffer<ValueEvent> ringBuffer =
        RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent()
        , BUFFER_SIZE
        , new YieldingWaitStrategy());
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly ValueMutationEventHandler_v3[] handlers = new ValueMutationEventHandler_v3[3];
        private readonly BatchEventProcessor<ValueEvent>[] batchEventProcessors = new BatchEventProcessor<ValueEvent>[3];

        public OneToThreeSequencedThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads(NUM_EVENT_PROCESSORS,NUM_EVENT_PROCESSORS);
            for (long i = 0; i < ITERATIONS; i++)
            {
                results[0] = Operation.Addition.Op(results[0], i);
                results[1] = Operation.Substraction.Op(results[1], i);
                results[2] = Operation.And.Op(results[2], i);
            }
            sequenceBarrier = ringBuffer.NewBarrier();

            handlers[0] = new ValueMutationEventHandler_v3(Operation.Addition);
            handlers[1] = new ValueMutationEventHandler_v3(Operation.Substraction);
            handlers[2] = new ValueMutationEventHandler_v3(Operation.And);

            batchEventProcessors[0] = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handlers[0]);
            batchEventProcessors[1] = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handlers[1]);
            batchEventProcessors[2] = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, handlers[2]);

            ringBuffer.AddGatingSequences(
                                     batchEventProcessors[0].Sequence,
                                     batchEventProcessors[1].Sequence,
                                     batchEventProcessors[2].Sequence);
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(NUM_EVENT_PROCESSORS);
            Enumerable.Range(0,NUM_EVENT_PROCESSORS).Select (i=>
            {
               
                //return Task.Run(() => batchEventProcessors[i].Run());
                return ThreadPool.UnsafeQueueUserWorkItem(o=>
                {
                    handlers[Convert.ToInt32(o)].reset(latch, batchEventProcessors[i].Sequence.Value + ITERATIONS);
                    batchEventProcessors[Convert.ToInt32(o)].Run()  ;
                },i);
                //return 1;
            }).ToList ();
            //handlers[0].reset(latch, batchEventProcessors[0].Sequence.Value + ITERATIONS);
            //Task.Run(() => batchEventProcessors[0].Run());
            //handlers[1].reset(latch, batchEventProcessors[1].Sequence.Value + ITERATIONS);
            //Task.Run(() => batchEventProcessors[1].Run());
            //handlers[2].reset(latch, batchEventProcessors[2].Sequence.Value + ITERATIONS);
            //Task.Run(() => batchEventProcessors[2].Run());

            Stopwatch start = Stopwatch.StartNew();

            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                ringBuffer[sequence].Value = i;
                ringBuffer.Publish(sequence);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            for (int i = 0; i < NUM_EVENT_PROCESSORS; i++)
            {
                batchEventProcessors[i].Halt();
                PerfTestUtil.failIfNot(results[i], handlers[i].Value);
            }

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
