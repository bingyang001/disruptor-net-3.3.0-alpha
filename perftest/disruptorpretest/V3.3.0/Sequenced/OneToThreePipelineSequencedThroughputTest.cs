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
 * Disruptor:
 * ==========
 *                           track to prevent wrap
 *              +----------------------------------------------------------------+
 *              |                                                                |
 *              |                                                                v
 * +----+    +====+    +=====+    +-----+    +=====+    +-----+    +=====+    +-----+
 * | P1 |--->| RB |    | SB1 |<---| EP1 |<---| SB2 |<---| EP2 |<---| SB3 |<---| EP3 |
 * +----+    +====+    +=====+    +-----+    +=====+    +-----+    +=====+    +-----+
 *      claim   ^  get    |   waitFor           |   waitFor           |  waitFor
 *              |         |                     |                     |
 *              +---------+---------------------+---------------------+
 *        </pre>
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB1 - SequenceBarrier 1
 * EP1 - EventProcessor 1
 * SB2 - SequenceBarrier 2
 * EP2 - EventProcessor 2
 * SB3 - SequenceBarrier 3
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
    public class OneToThreePipelineSequencedThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_EVENT_PROCESSORS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;
        private static readonly long OPERAND_TWO_INITIAL_VALUE = 777L;
        private  long expectedResult
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
        private readonly RingBuffer<FunctionEvent> ringBuffer =
       RingBuffer<FunctionEvent>.CreateSingleProducer(() => new FunctionEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private readonly ISequenceBarrier stepOneSequenceBarrier;
        private readonly FunctionEventHandler_v3 stepOneFunctionHandler = new FunctionEventHandler_v3(FunctionStep.One);

        private readonly BatchEventProcessor<FunctionEvent> stepOneBatchProcessor;
        private readonly ISequenceBarrier stepTwoSequenceBarrier;

        private readonly FunctionEventHandler_v3 stepTwoFunctionHandler = new FunctionEventHandler_v3(FunctionStep.Two);
        private readonly BatchEventProcessor<FunctionEvent> stepTwoBatchProcessor;

        private readonly ISequenceBarrier stepThreeSequenceBarrier;
        private readonly FunctionEventHandler_v3 stepThreeFunctionHandler = new FunctionEventHandler_v3(FunctionStep.Three);
        private readonly BatchEventProcessor<FunctionEvent> stepThreeBatchProcessor;
       
        public OneToThreePipelineSequencedThroughputTest()
            : base(Test_Disruptor, ITERATIONS,7)
        {
            ThreadPool.SetMaxThreads(NUM_EVENT_PROCESSORS, NUM_EVENT_PROCESSORS);
            stepOneSequenceBarrier = ringBuffer.NewBarrier();
            stepOneBatchProcessor = new BatchEventProcessor<FunctionEvent>(ringBuffer, stepOneSequenceBarrier, stepOneFunctionHandler);
            stepTwoSequenceBarrier = ringBuffer.NewBarrier(stepOneBatchProcessor.Sequence);

            stepTwoBatchProcessor = new BatchEventProcessor<FunctionEvent>(ringBuffer, stepTwoSequenceBarrier, stepTwoFunctionHandler);
            stepThreeSequenceBarrier = ringBuffer.NewBarrier(stepTwoBatchProcessor.Sequence);
            stepThreeBatchProcessor = new BatchEventProcessor<FunctionEvent>(ringBuffer, stepThreeSequenceBarrier, stepThreeFunctionHandler);

            ringBuffer.AddGatingSequences(stepThreeBatchProcessor.Sequence);

        }
        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            CountdownEvent latch = new CountdownEvent(1);
            stepThreeFunctionHandler.reset(latch, stepThreeBatchProcessor.Sequence.Value + ITERATIONS);

            Task.Run(() => stepOneBatchProcessor.Run());
            Task.Run(() => stepTwoBatchProcessor.Run());
            Task.Run(() => stepThreeBatchProcessor.Run());
            Stopwatch start = Stopwatch.StartNew();

            long operandTwo = OPERAND_TWO_INITIAL_VALUE;
            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                FunctionEvent @event = ringBuffer[sequence];
                @event.OperandOne = i;
                @event.OperandTwo = operandTwo--;
                ringBuffer.Publish(sequence);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);

            stepOneBatchProcessor.Halt();
            stepTwoBatchProcessor.Halt();
            stepThreeBatchProcessor.Halt();

            PerfTestUtil.failIfNot(expectedResult, stepThreeFunctionHandler.StepThreeCounter);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
