using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;
using disruptorpretest.Support;
using System.Threading.Tasks;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
    /// 
    /// <pre>
    /// Sequence a series of events from multiple publishers going to multiple work processors.
    ///
    /// +----+                  +-----+
    /// | P1 |---+          +-->| WP1 |
    /// +----+   |  +-----+ |   +-----+
    ///          +->| RB1 |-+
    /// +----+   |  +-----+ |   +-----+
    /// | P2 |---+          +-->| WP2 |
    /// +----+                  +-----+
    ///
    /// P1  - Publisher 1
    /// P2  - Publisher 2
    /// RB  - RingBuffer
    /// WP1 - EventProcessor 1
    /// WP2 - EventProcessor 2
    /// </pre>
    ///
    /// </summary>
    public class TwoPublisherToTwoProcessorWorkProcessorThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_PUBLISHERS = 2;
        private const int BUFFER_SIZE = 1024 * 64;
        private const long ITERATIONS = 1000L * 1000L * 100L;
        private Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);
        private Sequence workSequence = new Sequence(-1);
        private RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateMultiProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private ValueAdditionWorkHandler_V3[] handlers = { new ValueAdditionWorkHandler_V3(), new ValueAdditionWorkHandler_V3() };
        private WorkProcessor<ValueEvent>[] workProcessors = new WorkProcessor<ValueEvent>[2];
        //private WorkerPool<ValueEvent> workerPool;

        private ValuePublisher_V3[] valuePublishers = new ValuePublisher_V3[2];
        private ISequenceBarrier sequenceBarrier;

        public TwoPublisherToTwoProcessorWorkProcessorThroughputTest()
            : base(TestName, ITERATIONS)
        {
            sequenceBarrier = ringBuffer.NewBarrier();
            workProcessors[0] = new WorkProcessor<ValueEvent>(ringBuffer, sequenceBarrier,
                                                       handlers[0], new IgnoreExceptionHandler(),
                                                       workSequence);
            workProcessors[1] = new WorkProcessor<ValueEvent>(ringBuffer, sequenceBarrier,
                                                             handlers[1], new IgnoreExceptionHandler(),
                                                             workSequence);
            //workerPool = new WorkerPool<ValueEvent>(ringBuffer, sequenceBarrier, new IgnoreExceptionHandler(), handlers);
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                valuePublishers[i] = new ValuePublisher_V3(cyclicBarrier, ringBuffer, ITERATIONS);
            }
            workProcessors.ToList().ForEach(e => ringBuffer.AddGatingSequences(e.Sequence));
            //ringBuffer.AddGatingSequences(/*workerPool.getWorkerSequences()*/workProcessors[0].Sequence, workProcessors[1].Sequence);
        }


        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            //这里没有按java测试的代码写法做，太繁琐了，.net 简单
            var expected = ringBuffer.GetCursor + (NUM_PUBLISHERS * ITERATIONS);
            
            valuePublishers.ToList().ForEach(e => Task.Factory.StartNew(() => e.Run()));
            workProcessors.ToList().ForEach(e => Task.Factory.StartNew(() => e.Run()));
            //workerPool.start(TaskScheduler.Default);
            var runTime = System.Diagnostics.Stopwatch.StartNew();
            
            cyclicBarrier.SignalAndWait();

            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {

            }
            var spanWait = default(SpinWait);
            while (workSequence.Value < expected)
            {               
                spanWait.SpinOnce();
            }

            var opsPerSecond = (ITERATIONS * 1000L) / runTime.ElapsedMilliseconds;

            //Thread.Sleep(1000);
            workProcessors.ToList().ForEach(e => e.Halt());
            //workerPool.drainAndHalt();
            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
