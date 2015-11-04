/**
 * <pre>
 * Sequence a series of events from multiple publishers going to multiple work processors.
 *
 * +----+                  +-----+
 * | P1 |---+          +-->| WP1 |
 * +----+   |  +-----+ |   +-----+
 *          +->| RB1 |-+
 * +----+   |  +-----+ |   +-----+
 * | P2 |---+          +-->| WP2 |
 * +----+                  +-----+
 *
 * P1  - Publisher 1
 * P2  - Publisher 2
 * RB  - RingBuffer
 * WP1 - EventProcessor 1
 * WP2 - EventProcessor 2
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
using disruptorpretest.Support.V3._3._0;

namespace disruptorpretest.V3._3._0.Workhandler
{
    public class TwoToTwoWorkProcessorThroughputTest :
    AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_PUBLISHERS = 2;
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 1L;
        private readonly Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);
        private readonly RingBuffer<ValueEvent> ringBuffer =
       RingBuffer<ValueEvent>.CreateMultiProducer(() => new ValueEvent(), BUFFER_SIZE, new BusySpinWaitStrategy());
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly Sequence workSequence = new Sequence(-1);
        private readonly ValueAdditionWorkHandler[] handlers = new ValueAdditionWorkHandler[2];
        private readonly WorkProcessor<ValueEvent>[] workProcessors = new WorkProcessor<ValueEvent>[2];
        private readonly ValuePublisher[] valuePublishers = new ValuePublisher[NUM_PUBLISHERS];
        public TwoToTwoWorkProcessorThroughputTest()
            : base(Test_Disruptor, ITERATIONS)
        {
            sequenceBarrier = ringBuffer.NewBarrier();
            handlers[0] = new ValueAdditionWorkHandler();
            handlers[1] = new ValueAdditionWorkHandler();

            workProcessors[0] = new WorkProcessor<ValueEvent>(ringBuffer, sequenceBarrier,
                                                    handlers[0], new IgnoreExceptionHandler(),
                                                    workSequence);
            workProcessors[1] = new WorkProcessor<ValueEvent>(ringBuffer, sequenceBarrier,
                                                             handlers[1], new IgnoreExceptionHandler(),
                                                             workSequence);

            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                valuePublishers[i] = new ValuePublisher(cyclicBarrier, ringBuffer, ITERATIONS);
            }

            ringBuffer.AddGatingSequences(workProcessors[0].Sequence, workProcessors[1].Sequence);

        }
        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            long expected = ringBuffer.GetCursor + (NUM_PUBLISHERS * ITERATIONS);
            Task[] task = new Task[NUM_PUBLISHERS];
            //for (int i = 0; i < NUM_PUBLISHERS; i++)
            //{
            //    task[i] = Task.Run(() => valuePublishers[i].run());
            //}
            task[0] = Task.Run(() => valuePublishers[0].run());
            task[1] = Task.Run(() => valuePublishers[1].run());
            var start = Stopwatch.StartNew();
            cyclicBarrier.SignalAndWait();
            Task.WaitAll(task);
            while (workSequence.Value < expected)
            {
                Thread.Sleep(1);
            }
            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;

            Thread.Sleep(1000);
            foreach (WorkProcessor<ValueEvent> processor in workProcessors)
            {
                processor.Halt();
            }

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
