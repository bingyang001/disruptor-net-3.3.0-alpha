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
using System.Linq;
using System.Text;
using Disruptor;
using disruptorpretest.Support;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using NUnit.Framework;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
    /// 一个生产者三个消费者（菱形结构生产消费模型）吞吐量
    /// </summary>
    public class OnePublisherToThreeProcessorDiamondThroughputTest : AbstractPerfTestQueueVsDisruptor
    {

        private RingBuffer<FizzBuzzEvent> ringBuffer;
        private ISequenceBarrier sequenceBarrier;
        private ISequenceBarrier sequenceBarrierFizzBuzz;

        private FizzBuzzEventHandler_V3 fizzHandler = new FizzBuzzEventHandler_V3(FizzBuzzStep.Fizz);
        private BatchEventProcessor<FizzBuzzEvent> batchProcessorFizz;

        private FizzBuzzEventHandler_V3 buzzHandler = new FizzBuzzEventHandler_V3(FizzBuzzStep.Buzz);
        private BatchEventProcessor<FizzBuzzEvent> batchProcessorBuzz;

        private FizzBuzzEventHandler_V3 fizzBuzzHandler = new FizzBuzzEventHandler_V3(FizzBuzzStep.FizzBuzz);
        private BatchEventProcessor<FizzBuzzEvent> batchProcessorFizzBuzz;

        public OnePublisherToThreeProcessorDiamondThroughputTest()
            : base(TestName, 1000L * 1000L * 100L)
        {
            ringBuffer = RingBuffer<FizzBuzzEvent>.CreateSingleProducer(() => new FizzBuzzEvent(), 1024 * 8, new YieldingWaitStrategy());
            sequenceBarrier = ringBuffer.NewBarrier();

            batchProcessorFizz = new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrier, fizzHandler);

            batchProcessorBuzz = new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrier, buzzHandler);
            sequenceBarrierFizzBuzz = ringBuffer.NewBarrier(batchProcessorFizz.Sequence, batchProcessorBuzz.Sequence);

            batchProcessorFizzBuzz = new BatchEventProcessor<FizzBuzzEvent>(ringBuffer, sequenceBarrierFizzBuzz, fizzBuzzHandler);
            ringBuffer.AddGatingSequences(batchProcessorFizzBuzz.Sequence);

        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            var latch = new ManualResetEvent(false);
            fizzBuzzHandler.Reset(latch, batchProcessorFizzBuzz.Sequence.Value + ITERATIONS);

            Task.Factory.StartNew(() => batchProcessorFizz.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Task.Factory.StartNew(() => batchProcessorBuzz.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Task.Factory.StartNew(() => batchProcessorFizzBuzz.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            var run = DateTime.Now;//Stopwatch.StartNew();
            for (long i = 0; i < ITERATIONS; i++)
            {
                var sequence = ringBuffer.Next();
                ringBuffer[sequence].Value = i;
                ringBuffer.Publish(sequence);
            }
            latch.WaitOne();
            var opsPerSecond = (ITERATIONS * 1000L) / (long)DateTime.Now.Subtract(run).Milliseconds;

            batchProcessorFizz.Halt();
            batchProcessorBuzz.Halt();
            batchProcessorFizzBuzz.Halt();

            Assert.AreEqual(ExpectedResult, fizzBuzzHandler.FizzBuzzCounter);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }

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
    }
}
