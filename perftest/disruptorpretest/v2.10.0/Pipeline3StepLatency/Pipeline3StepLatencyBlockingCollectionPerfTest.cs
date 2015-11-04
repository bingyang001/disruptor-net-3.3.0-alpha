using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using disruptorpretest.Support;
using NUnit.Framework;

namespace disruptorpretest.Pipeline3StepLatency
{
    /// <summary>
    /// 此测试对应3.1.1中的 ThrottledOnePublisherToThreeProcessorPipelineLatencyTest.cs 中的disruptor 流水线测试对比
    /// </summary>
    [TestFixture]    
    public sealed class Pipeline3StepLatencyBlockingCollectionPerfTest:AbstractPipeline3StepLatencyPerfTest
    {
        private readonly BlockingCollection<long> _stepOneQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepTwoQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepThreeQueue = new BlockingCollection<long>(Size);

        private readonly LatencyStepQueueEventProcessor _stepOneQueueEventProcessor;
        private readonly LatencyStepQueueEventProcessor _stepTwoQueueEventProcessor;
        private readonly LatencyStepQueueEventProcessor _stepThreeQueueEventProcessor;

        public Pipeline3StepLatencyBlockingCollectionPerfTest()
            : base(30 * Million)
        {
            _stepOneQueueEventProcessor = new LatencyStepQueueEventProcessor(FunctionStep.One, _stepOneQueue, _stepTwoQueue, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepTwoQueueEventProcessor = new LatencyStepQueueEventProcessor(FunctionStep.Two, _stepTwoQueue, _stepThreeQueue, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepThreeQueueEventProcessor = new LatencyStepQueueEventProcessor(FunctionStep.Three, _stepThreeQueue, null, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
        }

        public override void RunPass()
        {
            _stepThreeQueueEventProcessor.Reset();

            new Thread(_stepOneQueueEventProcessor.Run) { Name = "Step 1 queues" }.Start();
            new Thread(_stepTwoQueueEventProcessor.Run) { Name = "Step 2 queues" }.Start();
            new Thread(_stepThreeQueueEventProcessor.Run) { Name = "Step 3 queues" }.Start();

            for (long i = 0; i < Iterations; i++)
            {
                _stepOneQueue.Add(Stopwatch.GetTimestamp());

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos)
                {
                    // busy spin
                }
            }

            while (!_stepThreeQueueEventProcessor.Done)
            {
                // busy spin
            }
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}