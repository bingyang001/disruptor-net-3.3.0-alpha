using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;
using Disruptor;
using disruptorpretest.Support;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
    /// 1p3C work pool模式吞吐量测算
    /// </summary>
    public class OnePublisherToThreeWorkerPoolThroughputReleasingTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_WORKERS = 3;
        private const int BUFFER_SIZE = 1024 * 8;
        private const long RunCount = 1000L * 1000L * 100L;
        private _Volatile.PaddedLong[] counters = new _Volatile.PaddedLong[NUM_WORKERS];
        private EventCountingAndReleasingWorkHandler_V3[] handlers = new EventCountingAndReleasingWorkHandler_V3[NUM_WORKERS];
        private readonly RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private WorkerPool<ValueEvent> workerPool;

        public OnePublisherToThreeWorkerPoolThroughputReleasingTest()
            : base(TestName, RunCount)
        {
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                counters[i] = default(_Volatile.PaddedLong);
            }
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                handlers[i] = new EventCountingAndReleasingWorkHandler_V3(counters, i);
            }
            workerPool = new WorkerPool<ValueEvent>(ringBuffer,
                                        ringBuffer.NewBarrier(),
                                        new FatalExceptionHandler(),
                                        handlers);
            ringBuffer.AddGatingSequences(workerPool.getWorkerSequences());
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            ResetCounters();
            var ringBuffer = workerPool.start(TestTaskScheduler);
            var runTime = System.Diagnostics.Stopwatch.StartNew();

            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                ringBuffer[sequence].Value =i;
                ringBuffer.Publish(sequence);
            }

            workerPool.drainAndHalt();

            // Workaround to ensure that the last worker(s) have completed after releasing their events
            // OnePublisherToThreeWorkerPoolThroughputTest 测试时，去掉 Thread.Sleep(1);
            Thread.Sleep(1);

            var opsPerSecond = (ITERATIONS * 1000L) / runTime.ElapsedMilliseconds;

           Assert.AreEqual(ITERATIONS, SumCounters());

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }

        private void ResetCounters()
        {
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                counters[i].WriteUnfenced(0L);
            }
        }

        private long SumCounters()
        {
            long sumJobs = 0L;
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                sumJobs += counters[i].ReadUnfenced();
            }

            return sumJobs;
        }
    }
}
