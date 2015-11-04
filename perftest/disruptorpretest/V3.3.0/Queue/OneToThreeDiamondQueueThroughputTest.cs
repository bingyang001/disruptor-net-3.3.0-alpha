/**
 * <pre>
 * Produce an event replicated to two event processors and fold back to a single third event processor.
 *
 *           +-----+
 *    +----->| EP1 |------+
 *    |      +-----+      |
 *    |                   v
 * +----+              +-----+
 * | P1 |              | EP3 |
 * +----+              +-----+
 *    |                   ^
 *    |      +-----+      |
 *    +----->| EP2 |------+
 *           +-----+
 *
 *
 * Queue Based:
 * ============
 *                 take       put
 *     put   +====+    +-----+    +====+  take
 *    +----->| Q1 |<---| EP1 |--->| Q3 |<------+
 *    |      +====+    +-----+    +====+       |
 *    |                                        |
 * +----+    +====+    +-----+    +====+    +-----+
 * | P1 |--->| Q2 |<---| EP2 |--->| Q4 |<---| EP3 |
 * +----+    +====+    +-----+    +====+    +-----+
 *
 * P1  - Publisher 1
 * Q1  - Queue 1
 * Q2  - Queue 2
 * Q3  - Queue 3
 * Q4  - Queue 4
 * EP1 - EventProcessor 1
 * EP2 - EventProcessor 2
 * EP3 - EventProcessor 3
 *
 *
 * Disruptor:
 * ==========
 *                    track to prevent wrap
 *              +-------------------------------+
 *              |                               |
 *              |                               v
 * +----+    +====+               +=====+    +-----+
 * | P1 |--->| RB |<--------------| SB2 |<---| EP3 |
 * +----+    +====+               +=====+    +-----+
 *      claim   ^  get               |   waitFor
 *              |                    |
 *           +=====+    +-----+      |
 *           | SB1 |<---| EP1 |<-----+
 *           +=====+    +-----+      |
 *              ^                    |
 *              |       +-----+      |
 *              +-------| EP2 |<-----+
 *             waitFor  +-----+
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB1 - SequenceBarrier 1
 * EP1 - EventProcessor 1
 * EP2 - EventProcessor 2
 * SB2 - SequenceBarrier 2
 * EP3 - EventProcessor 3
 *
 * </pre>
 */
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using disruptorpretest.Support.V3._3._0;
using disruptorpretest.Support;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace disruptorpretest.V3._3._0.Queue
{
    public class OneToThreeDiamondQueueThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;

        private long ExpectedResult
        {
            get
            {
                var temp = 0L;

                for (long i = 0; i < ITERATIONS; i++)
                {
                    var fizz = 0 == (i % 3L);
                    var buzz = 0 == (i % 5L);

                    if (fizz && buzz)
                    {
                        ++temp;
                    }
                }

                return temp;
            }
        }

        private readonly ConcurrentQueue<long> fizzInputQueue = new ConcurrentQueue<long>();
        private readonly ConcurrentQueue<long> buzzInputQueue = new ConcurrentQueue<long>();
        private readonly ConcurrentQueue<Boolean> fizzOutputQueue = new ConcurrentQueue<Boolean>();
        private readonly ConcurrentQueue<Boolean> buzzOutputQueue = new ConcurrentQueue<Boolean>();
        private readonly FizzBuzzQueueProcessor fizzQueueProcessor;
        private readonly FizzBuzzQueueProcessor buzzQueueProcessor;
        private readonly FizzBuzzQueueProcessor fizzBuzzQueueProcessor;

        public OneToThreeDiamondQueueThroughputTest()
            : base("All", ITERATIONS)
        {
            fizzQueueProcessor = new FizzBuzzQueueProcessor(FizzBuzzStep.Fizz, fizzInputQueue, buzzInputQueue, fizzOutputQueue, buzzOutputQueue, ITERATIONS - 1);
            buzzQueueProcessor = new FizzBuzzQueueProcessor(FizzBuzzStep.Buzz, fizzInputQueue, buzzInputQueue, fizzOutputQueue, buzzOutputQueue, ITERATIONS - 1);
            fizzBuzzQueueProcessor = new FizzBuzzQueueProcessor(FizzBuzzStep.FizzBuzz, fizzInputQueue, buzzInputQueue, fizzOutputQueue, buzzOutputQueue, ITERATIONS - 1);
        }
        protected override long RunQueuePass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            fizzBuzzQueueProcessor.Reset(latch);

            CancellationTokenSource cance=new CancellationTokenSource ();
            var t1= Task.Factory.StartNew(() => fizzQueueProcessor.Run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);
            var t2 = Task.Factory.StartNew(() => buzzQueueProcessor.Run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);
            var t3 = Task.Factory.StartNew(() => fizzBuzzQueueProcessor.Run(), cance.Token, TaskCreationOptions.None, TaskScheduler.Default);

            var run=Stopwatch.StartNew();
            for (long i = 0; i < ITERATIONS; i++)
            {               
                fizzInputQueue.Enqueue(i);
                buzzInputQueue.Enqueue(i);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / run.ElapsedMilliseconds;
            fizzQueueProcessor.Halt();
            buzzQueueProcessor.Halt();
            fizzBuzzQueueProcessor.Halt();
            
            cance.Cancel();
            
            PerfTestUtil.failIf(ExpectedResult, 0);

            return opsPerSecond;

        }

        protected override long RunDisruptorPass()
        {
            throw new NotImplementedException();
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
