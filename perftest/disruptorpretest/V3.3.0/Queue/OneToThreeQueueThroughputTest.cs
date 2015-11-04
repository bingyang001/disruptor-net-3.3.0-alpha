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
 *
 * Queue Based:
 * ============
 *                 take
 *   put     +====+    +-----+
 *    +----->| Q1 |<---| EP1 |
 *    |      +====+    +-----+
 *    |
 * +----+    +====+    +-----+
 * | P1 |--->| Q2 |<---| EP2 |
 * +----+    +====+    +-----+
 *    |
 *    |      +====+    +-----+
 *    +----->| Q3 |<---| EP3 |
 *           +====+    +-----+
 *
 * P1  - Publisher 1
 * Q1  - Queue 1
 * Q2  - Queue 2
 * Q3  - Queue 3
 * EP1 - EventProcessor 1
 * EP2 - EventProcessor 2
 * EP3 - EventProcessor 3
 *
 * </pre>
 */


using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using disruptorpretest.Support.V3._3._0;
using disruptorpretest.Support;
using System.Threading.Tasks;
using System.Diagnostics;

namespace disruptorpretest.V3._3._0.Queue
{
    public class OneToThreeQueueThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 1L;
        private readonly long[] results = new long[NUM_EVENT_PROCESSORS];
        private readonly ConcurrentQueue<long>[] blockingQueues = new ConcurrentQueue<long>[NUM_EVENT_PROCESSORS];
        private readonly ValueMutationQueueProcessor[] queueProcessors = new ValueMutationQueueProcessor[NUM_EVENT_PROCESSORS];

        public OneToThreeQueueThroughputTest()
            : base(Test_Queue, ITERATIONS)
        {
            for (long i = 0; i < ITERATIONS; i++)
            {
                results[0] = Operation.Addition.Op(results[0], i);
                results[1] = Operation.Substraction.Op(results[1], i);
                results[2] = Operation.And.Op(results[2], i);
            }
            blockingQueues[0] = new ConcurrentQueue<long>();
            blockingQueues[1] = new ConcurrentQueue<long>();
            blockingQueues[2] = new ConcurrentQueue<long>();

            queueProcessors[0] = new ValueMutationQueueProcessor(blockingQueues[0], Support.Operation.Addition, ITERATIONS - 1);
            queueProcessors[1] = new ValueMutationQueueProcessor(blockingQueues[1], Support.Operation.Substraction, ITERATIONS - 1);
            queueProcessors[2] = new ValueMutationQueueProcessor(blockingQueues[2], Support.Operation.And, ITERATIONS - 1);
        }
        protected override long RunQueuePass()
        {
            CountdownEvent latch = new CountdownEvent(NUM_EVENT_PROCESSORS);
            Task[] task = new Task[NUM_EVENT_PROCESSORS];
            CancellationTokenSource[] cancel = new CancellationTokenSource[NUM_EVENT_PROCESSORS];
            for (int i = 0; i < NUM_EVENT_PROCESSORS; i++)
            {
                queueProcessors[i].reset(latch);
                cancel[i] = new CancellationTokenSource();
                task[i] = Task.Factory.StartNew(() => queueProcessors[i].run(), cancel[i].Token);
            }

            var start = Stopwatch.StartNew();
            for (long i = 0; i < ITERATIONS; i++)
            {
                long value = i;
                foreach (var queue in blockingQueues)
                {
                    queue.Enqueue(value);
                }
            }
            latch.Wait();

            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            for (int i = 0; i < NUM_EVENT_PROCESSORS; i++)
            {
                queueProcessors[i].halt();
                cancel[i].Cancel();
                PerfTestUtil.failIf(queueProcessors[i].getValue(), -1);
            }

            return opsPerSecond;
        }

        protected override long RunDisruptorPass()
        {
            return 0;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
