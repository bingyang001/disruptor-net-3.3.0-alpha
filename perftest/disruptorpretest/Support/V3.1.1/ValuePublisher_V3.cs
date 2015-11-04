using System;
using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class ValuePublisher_V3
    {
        private readonly Barrier cyclicBarrier;
        private readonly RingBuffer<ValueEvent> ringBuffer;
        private readonly long iterations;

        public ValuePublisher_V3(Barrier cyclicBarrier, RingBuffer<ValueEvent> ringBuffer, long iterations)
        {
            this.cyclicBarrier = cyclicBarrier;
            this.ringBuffer = ringBuffer;
            this.iterations = iterations;
        }

        public void Run()
        {
            try
            {
                cyclicBarrier.SignalAndWait();

                for (long i = 0; i < iterations; i++)
                {
                    var sequence = ringBuffer.Next();
                    var @event = ringBuffer[sequence];
                    @event.Value = i;
                    ringBuffer.Publish(sequence);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException();
            }
        }

    }
}
