using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;


namespace DisruptorGeneralTest.Dsl.stubs
{
    public class StubPublisher
    {
        private volatile bool running = true;
        private volatile int publicationCount = 0;

        private RingBuffer<TestEvent> ringBuffer;

        public StubPublisher(RingBuffer<TestEvent> ringBuffer)
        {
            this.ringBuffer = ringBuffer;
        }

        public void Run()
        {
            while (running)
            {
                long sequence = ringBuffer.Next();
                //final TestEvent entry = ringBuffer.get(sequence);
                ringBuffer.Publish(sequence);
                publicationCount++;
            }
        }

        public int GetPublicationCount
        {
            get { return publicationCount; }
        }

        public void Halt()
        {
            running = false;
        }
    }
}
