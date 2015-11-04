using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;
using Disruptor.Tests.Support;
using DisruptorGeneralTest;

namespace Disruptor.ConsoleApp.Test
{
    class Program
    {
        private static readonly object obj = new object();

        unsafe static void Main(string[] args)
        {

            new FalseSharing().DoWork();

            Console.Read();

            //var i = sizeof(int);
            //Console.WriteLine(i);
            //var i = 0;
            //var j = 1;

            //var result= Interlocked.CompareExchange(ref i, 5, 0);

            //Console.WriteLine(result + " \t" + i);

            //Console.Read();

            //WaitStrategyTestUtil.assertWaitForWithDelayOf(50, new BusySpinWaitStrategy());
            //return;
            //var ep = new EventPublisherTest();
            //ep.shouldPublishEvent();
            //ep.shouldPublishEvent();
            //ep.ShouldTryPublishEvent();
        }
    }


    public class EventPublisherTest : IEventTranslator<LongEvent>
    {
        private static readonly int BUFFER_SIZE = 32;
        private readonly RingBuffer<LongEvent> ringBuffer = RingBuffer<LongEvent>.CreateSingleProducer/*CreateMultiProducer*/(() => new LongEvent(0L), BUFFER_SIZE);


        public void shouldPublishEvent()
        {
           // RingBuffer<LongEvent> ringBuffer = RingBuffer<LongEvent>.CreateMultiProducer(() => new LongEvent(0L), BUFFER_SIZE);
            ringBuffer.AddGatingSequences(new NoOpEventProcessor<LongEvent>(ringBuffer).Sequence);

            for (var i = 0; i < 4; i++)
            {
                ringBuffer.PublishEvent(this);
                ringBuffer.PublishEvent(this);


                Console.WriteLine("ringBuffer[0].Value=={0} ", ringBuffer[0].Value);
                Console.WriteLine("ringBuffer[1].Value=={0} ", ringBuffer[1].Value);
            }
        }
        public void ShouldTryPublishEvent()
        {
            //RingBuffer<LongEvent> ringBuffer = RingBuffer<LongEvent>.CreateMultiProducer(() => new LongEvent(0L), BUFFER_SIZE);
            ringBuffer.AddGatingSequences(new Sequence());
            for (var i = 0; i < BUFFER_SIZE; i++)
            {
                ringBuffer.TryPublishEvent(this);
            }

            for (var i = 0; i < BUFFER_SIZE; i++)
            {
                Console.WriteLine("ringBuffer[{0}].Value={1} {2}", i,ringBuffer[i].Value, i + 29L);
            }

            Console.WriteLine("{0}", ringBuffer.TryPublishEvent(this));
        }

        #region IEventTranslator<LongEvent> 成员

        public void TranslateTo(LongEvent @event, long sequence)
        {
            @event.Value = sequence + 29;
            Console.WriteLine("sequence={0} value={1}", sequence, @event.Value);
        }

        #endregion
    }
}
