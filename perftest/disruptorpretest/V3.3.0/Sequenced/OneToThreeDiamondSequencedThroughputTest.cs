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
    public class OneToThreeDiamondSequencedThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;
        private long expectedResult
        {
            get
            {
                long temp = 0L;

                for (long i = 0; i < ITERATIONS; i++)
                {
                    bool fizz = 0 == (i % 3L);
                    bool buzz = 0 == (i % 5L);

                    if (fizz && buzz)
                    {
                        ++temp;
                    }
                }

                return temp;
            }
        }
        private readonly ManualResetEvent mr = new ManualResetEvent(false);
        private readonly RingBuffer<FizzBuzzEvent> ringBuffer =
                RingBuffer<FizzBuzzEvent>.CreateSingleProducer(() => new FizzBuzzEvent(), BUFFER_SIZE, new YieldingWaitStrategy());

        private readonly ISequenceBarrier sequenceBarrier;
        private readonly FizzBuzzEventHandler2 fizzHandler;

        private readonly BatchEventProcessor<FizzBuzzEvent> batchProcessorFizz;
        private readonly FizzBuzzEventHandler2 buzzHandler;
        private readonly BatchEventProcessor<FizzBuzzEvent> batchProcessorBuzz;
        private readonly ISequenceBarrier sequenceBarrierFizzBuzz;

        private readonly FizzBuzzEventHandler2 fizzBuzzHandler;
        private readonly BatchEventProcessor<FizzBuzzEvent> batchProcessorFizzBuzz;

        public OneToThreeDiamondSequencedThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads(NUM_EVENT_PROCESSORS, NUM_EVENT_PROCESSORS);
            sequenceBarrier = ringBuffer.NewBarrier();
            fizzHandler = new FizzBuzzEventHandler2(FizzBuzzStep.Fizz);

            batchProcessorFizz =
            new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrier, fizzHandler);

            buzzHandler = new FizzBuzzEventHandler2(FizzBuzzStep.Buzz);
            batchProcessorBuzz =
        new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrier, buzzHandler);

            sequenceBarrierFizzBuzz =
       ringBuffer.NewBarrier(batchProcessorFizz.Sequence, batchProcessorBuzz.Sequence);

            fizzBuzzHandler = new FizzBuzzEventHandler2(FizzBuzzStep.FizzBuzz);
            batchProcessorFizzBuzz = new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrier, fizzBuzzHandler);

            ringBuffer.AddGatingSequences(batchProcessorFizzBuzz.Sequence);
        }
        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            fizzBuzzHandler.rset(latch, batchProcessorFizzBuzz.Sequence.Value + ITERATIONS);
            Task.Run(() => batchProcessorFizz.Run());
            Task.Run(() => batchProcessorBuzz.Run());
            Task.Run(() => batchProcessorFizzBuzz.Run());


            Stopwatch start = Stopwatch.StartNew();

            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                ringBuffer[sequence].Value = i;
                ringBuffer.Publish(sequence);
            }

            //mr.WaitOne ();
            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);

            batchProcessorFizz.Halt();
            batchProcessorBuzz.Halt();
            batchProcessorFizzBuzz.Halt();

            PerfTestUtil.failIfNot(expectedResult, fizzBuzzHandler.FizzBuzzCounter);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
