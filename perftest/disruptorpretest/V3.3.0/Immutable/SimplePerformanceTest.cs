using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class SimplePerformanceTest
    {
        private readonly RingBuffer<EventHolder> ringBuffer;
        private readonly EventHolderHandler eventHolderHandler;

        public SimplePerformanceTest()
        {
            ringBuffer = RingBuffer<EventHolder>.CreateSingleProducer(() => new EventHolder(), Constants.SIZE, new YieldingWaitStrategy());
            eventHolderHandler = new EventHolderHandler(new SimpleEventHandler());
        }

        private void doRun()
        {
            BatchEventProcessor<EventHolder> batchEventProcessor =
                    new BatchEventProcessor<EventHolder>(ringBuffer,
                            ringBuffer.NewBarrier(),
                            eventHolderHandler);
            ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);

            Thread t = new Thread(o => (o as BatchEventProcessor<EventHolder>).Run());
            t.Start(batchEventProcessor);

            long iterations = Constants.ITERATIONS;
            for (long l = 0; l < iterations; l++)
            {
                SimpleEvent e = new SimpleEvent(l, l, l, l);
                ringBuffer.PublishEvent(new EventTranslatorOneArg(), e);
            }

            while (batchEventProcessor.Sequence.Value != iterations - 1)
            {
                Thread.Sleep(0);
            }

            batchEventProcessor.Halt();
            t.Join();
        }

    }

    public class EventTranslatorOneArg : IEventTranslatorOneArg<EventHolder, SimpleEvent>
    {

        #region IEventTranslatorOneArg<EventHolder,SimpleEvent> 成员

        public void TranslateTo(EventHolder @event, long sequence, SimpleEvent arg0)
        {
            @event.@event = arg0;
        }

        #endregion
    }
}
