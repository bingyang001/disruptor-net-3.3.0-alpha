using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Disruptor;
using disruptorpretest.Support;
using NUnit.Framework;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
    /// 一个生产者三个消费者（三步流水线模式）
    ///
    /// <pre>
    ///
    /// Pipeline a series of stages from a publisher to ultimate event processor.
    /// Each event processor depends on the output of the event processor.
    ///
    /// +----+    +-----+    +-----+    +-----+
    /// | P1 |--->| EP1 |--->| EP2 |--->| EP3 |
    /// +----+    +-----+    +-----+    +-----+
    ///
    ///
    /// Queue Based:
    /// ============
    ///
    ///        put      take        put      take        put      take
    /// +----+    +====+    +-----+    +====+    +-----+    +====+    +-----+
    /// | P1 |--->| Q1 |<---| EP1 |--->| Q2 |<---| EP2 |--->| Q3 |<---| EP3 |
    /// +----+    +====+    +-----+    +====+    +-----+    +====+    +-----+
    ///
    /// P1  - Publisher 1
    /// Q1  - Queue 1
    /// EP1 - EventProcessor 1
    /// Q2  - Queue 2
    /// EP2 - EventProcessor 2
    /// Q3  - Queue 3
    /// EP3 - EventProcessor 3
    ///
    ///
    /// Disruptor:
    /// ==========
    ///                           track to prevent wrap
    ///              +----------------------------------------------------------------+
    ///              |                                                                |
    ///              |                                                                v
    /// +----+    +====+    +=====+    +-----+    +=====+    +-----+    +=====+    +-----+
    /// | P1 |--->| RB |    | SB1 |<---| EP1 |<---| SB2 |<---| EP2 |<---| SB3 |<---| EP3 |
    /// +----+    +====+    +=====+    +-----+    +=====+    +-----+    +=====+    +-----+
    ///      claim   ^  get    |   waitFor           |   waitFor           |  waitFor
    ///              |         |                     |                     |
    ///              +---------+---------------------+---------------------+
    ///        </pre>
    ///
    /// P1  - Publisher 1
    /// RB  - RingBuffer
    /// SB1 - SequenceBarrier 1
    /// EP1 - EventProcessor 1
    /// SB2 - SequenceBarrier 2
    /// EP2 - EventProcessor 2
    /// SB3 - SequenceBarrier 3
    /// EP3 - EventProcessor 3
    ///
    /// </pre>
    ///
    /// </summary>
    public class OnePublisherToThreeProcessorPipelineThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_EVENT_PROCESSORS = 3;
        private const int BUFFER_SIZE = 1024 * 8;
        private long OPERAND_TWO_INITIAL_VALUE = 777L;
        private long expectedResult;


        private readonly RingBuffer<FunctionEvent> ringBuffer = RingBuffer<FunctionEvent>.CreateSingleProducer(() => new FunctionEvent(), 1024 * 8, new YieldingWaitStrategy());

        private ISequenceBarrier stepOneSequenceBarrier;
        private FunctionEventHandler_V3 stepOneFunctionHandler = new FunctionEventHandler_V3(FunctionStep.One);
        private BatchEventProcessor<FunctionEvent> stepOneBatchProcessor;

        private ISequenceBarrier stepTwoSequenceBarrier;
        private FunctionEventHandler_V3 stepTwoFunctionHandler = new FunctionEventHandler_V3(FunctionStep.Two);
        private BatchEventProcessor<FunctionEvent> stepTwoBatchProcessor;


        private ISequenceBarrier stepThreeSequenceBarrier;
        private FunctionEventHandler_V3 stepThreeFunctionHandler = new FunctionEventHandler_V3(FunctionStep.Three);
        private BatchEventProcessor<FunctionEvent> stepThreeBatchProcessor;



        public OnePublisherToThreeProcessorPipelineThroughputTest()
            : base(TestName, ITERATIONS)
        {
            InitResult();

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
            var latch = new ManualResetEvent(false);
            stepThreeFunctionHandler.Reset(latch, stepThreeBatchProcessor.Sequence.Value + ITERATIONS);

            Task.Factory.StartNew(() => stepOneBatchProcessor.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Task.Factory.StartNew(() => stepTwoBatchProcessor.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Task.Factory.StartNew(() => stepThreeBatchProcessor.Run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

            var runTime = Stopwatch.StartNew();
            var operandTwo = OPERAND_TWO_INITIAL_VALUE;
            for (long i = 0; i < ITERATIONS; i++)
            {
                var sequence = ringBuffer.Next();
                var @event = ringBuffer[sequence];
                @event.OperandOne = i;
                @event.OperandTwo = operandTwo--;
                ringBuffer.Publish(sequence);
            }
            latch.WaitOne();
            var opsPerSecond = (ITERATIONS * 1000L) / runTime.ElapsedMilliseconds;

            stepOneBatchProcessor.Halt();
            stepTwoBatchProcessor.Halt();
            stepThreeBatchProcessor.Halt();
            InitResult();
            Assert.AreEqual(expectedResult, stepThreeFunctionHandler.GetStepThreeCounter);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }

        private void InitResult()
        {
            var temp = 0L;
            var operandTwo = OPERAND_TWO_INITIAL_VALUE;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long stepOneResult = i + operandTwo--;
                long stepTwoResult = stepOneResult + 3;

                if ((stepTwoResult & 4L) == 4L)
                {
                    ++temp;
                }
            }

            expectedResult = temp;
        }
    }
}
