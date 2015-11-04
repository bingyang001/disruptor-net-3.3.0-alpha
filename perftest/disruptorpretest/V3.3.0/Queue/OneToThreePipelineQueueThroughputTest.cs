/**
 * <pre>
 *
 * Pipeline a series of stages from a publisher to ultimate event processor.
 * Each event processor depends on the output of the event processor.
 *
 * +----+    +-----+    +-----+    +-----+
 * | P1 |--->| EP1 |--->| EP2 |--->| EP3 |
 * +----+    +-----+    +-----+    +-----+
 *
 *
 * Queue Based:
 * ============
 *
 *        put      take        put      take        put      take
 * +----+    +====+    +-----+    +====+    +-----+    +====+    +-----+
 * | P1 |--->| Q1 |<---| EP1 |--->| Q2 |<---| EP2 |--->| Q3 |<---| EP3 |
 * +----+    +====+    +-----+    +====+    +-----+    +====+    +-----+
 *
 * P1  - Publisher 1
 * Q1  - Queue 1
 * EP1 - EventProcessor 1
 * Q2  - Queue 2
 * EP2 - EventProcessor 2
 * Q3  - Queue 3
 * EP3 - EventProcessor 3
 *
 * </pre>
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using disruptorpretest.Support;
using disruptorpretest.Support.V3._3._0;

namespace disruptorpretest.V3._3._0.Queue
{
    public class OneToThreePipelineQueueThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 10L;
        private static readonly long OPERAND_TWO_INITIAL_VALUE = 777L;

        private long expectedResult
        {
            get
            {
                long temp = 0L;
                long operandTwo = OPERAND_TWO_INITIAL_VALUE;
                for (long i = 0; i < ITERATIONS; i++)
                {
                    long stepOneResult = i + operandTwo--;
                    long stepTwoResult = stepOneResult + 3;

                    if ((stepTwoResult & 4L) == 4L)
                    {
                        ++temp;
                    }
                }
                return temp;
            }
        }

        private readonly ConcurrentQueue<long[]> stepOneQueue = new ConcurrentQueue<long[]>();
        private readonly ConcurrentQueue<long> stepTwoQueue = new ConcurrentQueue<long>();
        private readonly ConcurrentQueue<long> stepThreeQueue = new ConcurrentQueue<long>();

        private readonly FunctionQueueProcessor stepOneQueueProcessor;
        private readonly FunctionQueueProcessor stepTwoQueueProcessor;
        private readonly FunctionQueueProcessor stepThreeQueueProcessor;

        public OneToThreePipelineQueueThroughputTest()
            : base("ALL", ITERATIONS)
        {
            stepOneQueueProcessor = new FunctionQueueProcessor(FunctionStep.One, stepOneQueue, stepTwoQueue, stepThreeQueue, ITERATIONS - 1);
            stepTwoQueueProcessor = new FunctionQueueProcessor(FunctionStep.Two, stepOneQueue, stepTwoQueue, stepThreeQueue, ITERATIONS - 1);
            stepThreeQueueProcessor = new FunctionQueueProcessor(FunctionStep.Three, stepOneQueue, stepTwoQueue, stepThreeQueue, ITERATIONS - 1);
        }


        protected override long RunQueuePass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            stepThreeQueueProcessor.reset(latch);

            CancellationTokenSource cance = new CancellationTokenSource();
            var t1 = Task.Factory.StartNew(() => stepOneQueueProcessor.run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);
            var t2 = Task.Factory.StartNew(() => stepTwoQueueProcessor.run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);
            var t3 = Task.Factory.StartNew(() => stepThreeQueueProcessor.run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);
            var start = Stopwatch.StartNew();

            long operandTwo = OPERAND_TWO_INITIAL_VALUE;
            for (long i = 0; i < ITERATIONS; i++)
            {
                long[] values = new long[2];
                values[0] = i;
                values[1] = operandTwo--;
                stepOneQueue.Enqueue(values);
            }
            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            stepOneQueueProcessor.halt();
            stepTwoQueueProcessor.halt();
            stepThreeQueueProcessor.halt();

            cance.Cancel();

            PerfTestUtil.failIf(expectedResult, 0);

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
