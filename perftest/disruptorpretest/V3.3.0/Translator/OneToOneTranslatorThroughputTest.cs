/**
 * <pre>
 * UniCast a series of items between 1 publisher and 1 event processor using the EventTranslator API
 *
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *
 * Disruptor:
 * ==========
 *              track to prevent wrap
 *              +------------------+
 *              |                  |
 *              |                  v
 * +----+    +====+    +====+   +-----+
 * | P1 |--->| RB |<---| SB |   | EP1 |
 * +----+    +====+    +====+   +-----+
 *      claim      get    ^        |
 *                        |        |
 *                        +--------+
 *                          waitFor
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB  - SequenceBarrier
 * EP1 - EventProcessor 1
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
using Disruptor.Dsl;
using Disruptor.Utils;
using disruptorpretest.Support;
using ValueAdditionEventHandler = disruptorpretest.Support.V3._3._0.ValueAdditionEventHandler;
namespace disruptorpretest.V3._3._0.Translator
{
    public class OneToOneTranslatorThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 100L;
        private readonly long expectedResult = PerfTestUtil.AccumulatedAddition(ITERATIONS);
        private readonly ValueAdditionEventHandler handler = new ValueAdditionEventHandler();
        private readonly RingBuffer<ValueEvent> ringBuffer;
        private readonly MutableLong value = new MutableLong(0);
        public OneToOneTranslatorThroughputTest() : base(Test_Disruptor, ITERATIONS) 
        {
            Disruptor<ValueEvent> disruptor =
                   new Disruptor<ValueEvent>(()=>new ValueEvent(),
                                             BUFFER_SIZE, TaskScheduler.Default,
                                             ProducerType.SINGLE,
                                             new YieldingWaitStrategy());
            disruptor.HandleEventsWith(handler);
            this.ringBuffer = disruptor.Start();
         }
        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            MutableLong value = this.value;

            CountdownEvent latch = new CountdownEvent(1);
            long expectedCount = ringBuffer.GetMinimumGatingSequence() + ITERATIONS;

            handler.reset(latch, expectedCount);
            var start = Stopwatch.StartNew();
            RingBuffer<ValueEvent> rb = ringBuffer;
            for (long l = 0; l < ITERATIONS; l++)
            {
                value.Value = l;
                rb.PublishEvent(Translator.INSTANCE, value);
            }
            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            waitForEventProcessorSequence(expectedCount);

            PerfTestUtil.failIfNot(expectedResult, handler.Value);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
        private void waitForEventProcessorSequence(long expectedCount)
        {
            while (ringBuffer.GetMinimumGatingSequence() != expectedCount)
            {
                Thread.Sleep(1);
            }
        }
    }
    internal class Translator : IEventTranslatorOneArg<ValueEvent, MutableLong>
    {
        public static readonly Translator INSTANCE = new Translator();


        public void TranslateTo(ValueEvent @event, long sequence, MutableLong arg0)
        {
            @event.Value = arg0.Value;
        }
    }
}
