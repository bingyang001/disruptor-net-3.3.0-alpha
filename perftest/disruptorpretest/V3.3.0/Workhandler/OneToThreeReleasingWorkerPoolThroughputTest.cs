using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using disruptorpretest.Support;
using disruptorpretest.Support.V3._3._0;

namespace disruptorpretest.V3._3._0.Workhandler
{
    public class OneToThreeReleasingWorkerPoolThroughputTest 
    : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_WORKERS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 8;
        private static readonly long ITERATIONS = 1000L * 1000 * 10L;
        private readonly RingBuffer<ValueEvent> ringBuffer =
                RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(),
                                                BUFFER_SIZE,
                                                new YieldingWaitStrategy());
        private readonly WorkerPool<ValueEvent> workerPool;
        private readonly _Volatile.PaddedLong[] counters = new _Volatile.PaddedLong[NUM_WORKERS];
        private readonly EventCountingAndReleasingWorkHandler[] handlers = new EventCountingAndReleasingWorkHandler[NUM_WORKERS];

        public OneToThreeReleasingWorkerPoolThroughputTest()
            : base(Test_Disruptor, ITERATIONS)
        {
            ThreadPool.SetMaxThreads(NUM_WORKERS, NUM_WORKERS);
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                counters[i] = new _Volatile.PaddedLong();
                handlers[i] = new EventCountingAndReleasingWorkHandler(counters, i);
            }
            workerPool = new WorkerPool<ValueEvent>(ringBuffer
                                           , ringBuffer.NewBarrier()
                                           ,new FatalExceptionHandler()
                                           ,handlers);
            
            ringBuffer.AddGatingSequences(workerPool.getWorkerSequences());
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            resetCounters();
            RingBuffer<ValueEvent> ringBuffer = workerPool.start(TaskScheduler.Default);
            var start = Stopwatch.StartNew();
            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                ringBuffer.Get(sequence).Value = i;
                ringBuffer.Publish(sequence);
            }

            workerPool.drainAndHalt();
            Thread.Sleep(1);

            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;

            PerfTestUtil.failIfNot(ITERATIONS, sumCounters());

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }

        private void resetCounters()
        {
            for (int i = 0; i < NUM_WORKERS; i++)
            {
                counters[i].WriteUnfenced(0L);
            }
        }

        private long sumCounters()
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
