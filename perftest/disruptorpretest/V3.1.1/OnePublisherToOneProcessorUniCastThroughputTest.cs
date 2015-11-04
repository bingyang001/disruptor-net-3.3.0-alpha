using System;
using System.Collections.Generic;
using System.Collections.Concurrent ;
using System.Linq;
using System.Text;
using disruptorpretest.Support;
using Disruptor;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using NUnit.Framework;

namespace disruptorpretest.V3._1._1
{
    public class OnePublisherToOneProcessorUniCastThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private readonly long expectedResult = PerfTestUtil.AccumulatedAddition(ITERATIONS);
        private readonly RingBuffer<ValueEvent> ringBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private readonly ValueAdditionEventHandler_V3 disruptorHandler = new ValueAdditionEventHandler_V3();
        private readonly ValueAdditionQueueProcessor_V3 queueHandler;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly BatchEventProcessor<ValueEvent> batchEventProcessor;
        private readonly BlockingCollection<long> blockQueue = new BlockingCollection<long>();

        public OnePublisherToOneProcessorUniCastThroughputTest()
            : base("Disruptor", ITERATIONS)
        {
            sequenceBarrier = ringBuffer.NewBarrier();
            batchEventProcessor = new BatchEventProcessor<ValueEvent>(ringBuffer, sequenceBarrier, disruptorHandler);
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);

            //queueHandler = new ValueAdditionQueueProcessor_V3(blockQueue, ITERATIONS - 1);
        }



        protected override long RunQueuePass()
        {
            var latch = new ManualResetEvent(false);
            queueHandler.Reset(latch);
            Task.Factory.StartNew(queueHandler.Run, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            var runTime = Stopwatch.StartNew();
            for (var i = 0L; i < ITERATIONS; i++)
            {
                blockQueue.Add(i);
            }
            latch.WaitOne();
            var opsPerSecond = (ITERATIONS * 1000L) / runTime.ElapsedMilliseconds ;
            queueHandler.Halt();
            Assert.AreEqual(expectedResult, queueHandler.Value);

            return opsPerSecond;
        }

        protected override long RunDisruptorPass()
        {
            var latch = new ManualResetEvent(false);
            var expectedCount = batchEventProcessor.Sequence.Value + ITERATIONS;
            disruptorHandler.Reset(latch, expectedCount);

            Task.Factory.StartNew(batchEventProcessor.Run, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            var start = Stopwatch.StartNew();

            var rb = ringBuffer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = rb.Next();
                rb[next].Value = i;
                rb.Publish(next);
            }

            latch.WaitOne();
            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;
            SpinWait.SpinUntil(()=>batchEventProcessor.Sequence.Value == expectedCount);
            //WaitForEventProcessorSequence(expectedCount);
            batchEventProcessor.Halt();


            Assert.AreEqual(expectedResult, disruptorHandler.Value);

            return opsPerSecond;
        }
       
        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
