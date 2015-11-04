using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValuePublisher
    {
        private readonly Barrier cyclicBarrier;
        private readonly RingBuffer<ValueEvent> ringBuffer;
        private readonly long iterations;
        public ValuePublisher(Barrier cyclicBarrier
        , RingBuffer<ValueEvent> ringBuffer
        , long iterations)
        {
            this.cyclicBarrier = cyclicBarrier;
            this.ringBuffer = ringBuffer;
            this.iterations = iterations;
        }
        public void run()
        {
            try
            {
                cyclicBarrier.SignalAndWait();

                for (long i = 0; i < iterations; i++)
                {
                    long sequence = ringBuffer.Next();
                    ValueEvent @event = ringBuffer.Get(sequence);
                    @event.Value = (i);
                    ringBuffer.Publish(sequence);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
