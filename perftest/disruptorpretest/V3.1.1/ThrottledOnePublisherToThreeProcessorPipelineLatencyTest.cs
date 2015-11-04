using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

using Disruptor;
using disruptorpretest.Support;
using Disruptor.Collections;

using NUnit.Framework;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
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
    /// Note: <b>This test is only useful on a system using an invariant TSC in user space from the System.nanoTime() call.</b>
    ///
    /// </summary>
    [TestFixture]
    public class ThrottledOnePublisherToThreeProcessorPipelineLatencyTest //: AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_EVENT_PROCESSORS = 3;
        private const int BUFFER_SIZE = 1024 * 8;
        private const long ITERATIONS = 1000L * 1000L * 30L;
        private const long PAUSE_NANOS = 1000L;
        private Histogram _histogram;
        protected static readonly long TicksToNanos = 1000 * 1000 * 1000 / Stopwatch.Frequency;

        private RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());

        private ISequenceBarrier stepOneSequenceBarrier;
        private LatencyStepEventHandler_V3 stepOneFunctionHandler;
        private BatchEventProcessor<ValueEvent> stepOneBatchProcessor;

        private ISequenceBarrier stepTwoSequenceBarrier;
        private LatencyStepEventHandler_V3 stepTwoFunctionHandler;
        private BatchEventProcessor<ValueEvent> stepTwoBatchProcessor;

        private ISequenceBarrier stepThreeSequenceBarrier;
        private LatencyStepEventHandler_V3 stepThreeFunctionHandler;
        private BatchEventProcessor<ValueEvent> stepThreeBatchProcessor;


        public ThrottledOnePublisherToThreeProcessorPipelineLatencyTest()
        //: base(TestName, ITERATIONS)
        {
            InitHistogram();

            stepOneSequenceBarrier = ringBuffer.NewBarrier();
            stepOneFunctionHandler = new LatencyStepEventHandler_V3(FunctionStep.One, _histogram, TicksToNanos);
            stepOneBatchProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, stepOneSequenceBarrier, stepOneFunctionHandler);

            stepTwoSequenceBarrier = ringBuffer.NewBarrier(stepOneBatchProcessor.Sequence);
            stepTwoFunctionHandler = new LatencyStepEventHandler_V3(FunctionStep.Two, _histogram, TicksToNanos);
            stepTwoBatchProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, stepTwoSequenceBarrier, stepTwoFunctionHandler);

            stepThreeSequenceBarrier = ringBuffer.NewBarrier(stepTwoBatchProcessor.Sequence);
            stepThreeFunctionHandler = new LatencyStepEventHandler_V3(FunctionStep.Three, _histogram, TicksToNanos);
            stepThreeBatchProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, stepThreeSequenceBarrier, stepThreeFunctionHandler);

            ringBuffer.AddGatingSequences(stepThreeBatchProcessor.Sequence);
        }

        [Test]
        public void RunDisruptorPassTest()
        {
            var runs = 3;

            var queueMeanLatency = new decimal[runs];
            var disruptorMeanLatency = new decimal[runs];
            for (int i = 0; i < runs; i++)
            {
                // System.gc();
                _histogram.Clear();

                RunDisruptorPass();

                Assert.AreEqual(_histogram.Count, ITERATIONS);
                disruptorMeanLatency[i] = _histogram.Mean;

                Console.WriteLine("run {0} Disruptor {1}", i, _histogram.ToString());
                DumpHistogram();
            }

            for (int i = 0; i < runs; i++)
            {
               // Assert.True(queueMeanLatency[i].CompareTo(disruptorMeanLatency[i]) > 0);
                Console.WriteLine(disruptorMeanLatency[i]);
            }
        }

        private void RunDisruptorPass()
        {
            var latch = new System.Threading.ManualResetEvent(false);
            stepThreeFunctionHandler.Reset(latch, stepThreeBatchProcessor.Sequence.Value + ITERATIONS);

            Task.Factory.StartNew(() => stepOneBatchProcessor.Run());
            Task.Factory.StartNew(() => stepTwoBatchProcessor.Run());
            Task.Factory.StartNew(() => stepThreeBatchProcessor.Run());

            for (long i = 0; i < ITERATIONS; i++)
            {
                var t0 = Stopwatch.GetTimestamp();
                var sequence = ringBuffer.Next();
                ringBuffer[sequence].Value = t0;
                ringBuffer.Publish(sequence);

                var pauseStart = Stopwatch.GetTimestamp();
                while (PAUSE_NANOS > ((Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos))
                {
                    // busy spin
                }
            }           
            latch.WaitOne();
            stepOneBatchProcessor.Halt();
            stepTwoBatchProcessor.Halt();
            stepThreeBatchProcessor.Halt();
        }



        private void InitHistogram()
        {

            if (_histogram == null)
            {
                var intervals = new long[31];
                var intervalUpperBound = 1L;
                for (var i = 0; i < intervals.Length - 1; i++)
                {
                    intervalUpperBound *= 2;
                    intervals[i] = intervalUpperBound;
                }

                intervals[intervals.Length - 1] = long.MaxValue;
                _histogram = new Histogram(intervals);
            }
        }

        private void DumpHistogram()
        {
            for (int i = 0, size = _histogram.Size; i < size; i++)
            {
                Console.WriteLine("{0} {1}, {2}", i,_histogram.GetUpperBoundAt(i), _histogram.GetCountAt(i));                                             
            }
        }
    }
}
