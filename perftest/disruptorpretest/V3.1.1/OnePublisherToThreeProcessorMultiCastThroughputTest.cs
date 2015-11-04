using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using disruptorpretest.Support;
using NUnit.Framework;
using Disruptor.Dsl;


namespace disruptorpretest.V3._1._1
{    
    public class OnePublisherToThreeProcessorMultiCastThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int NUM_EVENT_PROCESSORS = 3;

        //private readonly RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent()
        //    , 1024 * 8, new YieldingWaitStrategy());
        private Disruptor<ValueEvent> disruptor;
        private RingBuffer<ValueEvent> ringBuffer;

        private ISequenceBarrier sequenceBarrier;

        private ValueMutationEventHandler_V3[] handlers = new ValueMutationEventHandler_V3[NUM_EVENT_PROCESSORS];
        private BatchEventProcessor<ValueEvent>[] batchEventProcessors = new BatchEventProcessor<ValueEvent>[NUM_EVENT_PROCESSORS];

        private long[] results;
        public OnePublisherToThreeProcessorMultiCastThroughputTest()
            : base(TestName, ITERATIONS)
        {

            results = new long[3];
            for (long i = 0; i < ITERATIONS; i++)
            {
                results[0] = Operation.Addition.Op(results[0], i);
                results[1] = Operation.Substraction.Op(results[1], i);
                results[2] = Operation.And.Op(results[2], i);
            }

            handlers[0] = new ValueMutationEventHandler_V3(Operation.Addition);
            handlers[1] = new ValueMutationEventHandler_V3(Operation.Substraction);
            handlers[2] = new ValueMutationEventHandler_V3(Operation.And);
        }

      //  [SetUp]
        public void Setup()
        {
            disruptor = new Disruptor<ValueEvent>(() => new ValueEvent()
              , 1024 * 8
              , TaskScheduler.Default
              , ProducerType.SINGLE
              , new YieldingWaitStrategy()
              );
            disruptor.HandleEventsWith(handlers);
            ringBuffer = disruptor.GetRingBuffer;
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            Setup();
            var latch = new CountdownEvent(NUM_EVENT_PROCESSORS);
            var listTh = new List<Thread>();
            for (int i = 0; i < 3; i++)
            {
                handlers[i].Reset(latch, -1 + ITERATIONS);
            }

            disruptor.Start();
            var start = System.Diagnostics.Stopwatch.StartNew();

            for (long i = 0; i < ITERATIONS; i++)
            {
                long sequence = ringBuffer.Next();
                ringBuffer[sequence].Value = i;
                ringBuffer.Publish(sequence);
            }
            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;
            for (int i = 0; i < NUM_EVENT_PROCESSORS; i++)
            {
                Assert.AreEqual(results[i], handlers[i].Value);
            }
            disruptor.Shutdown();
            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
