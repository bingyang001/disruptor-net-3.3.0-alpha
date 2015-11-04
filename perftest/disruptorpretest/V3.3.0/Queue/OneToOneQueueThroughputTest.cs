
/**
 * <pre>
 * UniCast a series of items between 1 publisher and 1 event processor.
 *
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
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
 * </pre>
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using disruptorpretest.Support;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace disruptorpretest.V3._3._0.Queue
{
    public class OneToOneQueueThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 10L;
        //private readonly ExecutorService executor = Executors.newSingleThreadExecutor(DaemonThreadFactory.INSTANCE);
        private readonly long expectedResult = PerfTestUtil.AccumulatedAddition(ITERATIONS);
        private readonly BlockingCollection<long> blockingQueue = new BlockingCollection<long>(BUFFER_SIZE);
        private readonly ValueAdditionQueueProcessor queueProcessor;
        public OneToOneQueueThroughputTest()
            : base(Test_Queue, ITERATIONS)
        {
        ThreadPool.SetMaxThreads (1,1);
            queueProcessor = new ValueAdditionQueueProcessor(blockingQueue, ITERATIONS - 1);
        }
        protected override long RunQueuePass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            queueProcessor.Reset(latch);
            CancellationTokenSource canncel = new CancellationTokenSource();

            Task.Factory.StartNew(() => queueProcessor.Run(), canncel.Token);

            var start = Stopwatch.StartNew();

            for (long i = 0; i < ITERATIONS; i++)
            {
                blockingQueue.Add(i);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            queueProcessor.Halt();
            canncel.Cancel(true);

            PerfTestUtil.failIf(expectedResult, 0);

            return opsPerSecond;
        }

        protected override long RunDisruptorPass()
        {
            return 1;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
