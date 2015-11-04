using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

using NUnit.Framework;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class EventPublisherTest : IEventTranslator<LongEvent>
    {
        private static readonly int BUFFER_SIZE = 32;
        //private readonly RingBuffer<LongEvent> ringBuffer = RingBuffer<LongEvent>.CreateSingleProducer(() => new LongEvent(0L), BUFFER_SIZE);

        [Test]
        public void ShouldPublishEvent()
        {
            var ringBuffer = RingBuffer<LongEvent>.CreateSingleProducer(() => new LongEvent(0L), BUFFER_SIZE);
            ringBuffer.AddGatingSequences(new NoOpEventProcessor<LongEvent>(ringBuffer).Sequence);

            ringBuffer.PublishEvent(this);
            ringBuffer.PublishEvent(this);
           
            Assert.AreEqual(ringBuffer[0].Value, 29L);
            Assert.AreEqual(ringBuffer[1].Value, 30L);
        }

        [Test]
        public void ShouldTryPublishEvent()
        {
            var ringBuffer = RingBuffer<LongEvent>.CreateSingleProducer(() => new LongEvent(0L), BUFFER_SIZE);
            ringBuffer.AddGatingSequences(new Sequence());
            for (var i = 0; i < BUFFER_SIZE; i++)
            {
                Assert.AreEqual(ringBuffer.TryPublishEvent(this), true);
            }

            for (var i = 0; i < BUFFER_SIZE; i++)
            {
                Assert.AreEqual(ringBuffer[i].Value, i + 29L);
            }

            Assert.AreEqual(ringBuffer.TryPublishEvent(this), false);
        }

        #region IEventTranslator<LongEvent> 成员

        public void TranslateTo(LongEvent @event, long sequence)
        {
            @event.Value = sequence + 29;
            //Console.WriteLine("sequenc= {0} value={1}", sequence, @event.Value);
        }

        #endregion
    }   
}
