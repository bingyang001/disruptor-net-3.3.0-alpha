using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{

    public class LongArrayPublisher
    {
        private readonly Barrier cyclicBarrier;
        private readonly RingBuffer<long[]> ringBuffer;
        private readonly long iterations;
        private readonly long arraySize;

        public LongArrayPublisher(Barrier cyclicBarrier,
                             RingBuffer<long[]> ringBuffer,
                              long iterations,
                              long arraySize)
        {
            this.cyclicBarrier = cyclicBarrier;
            this.ringBuffer = ringBuffer;
            this.iterations = iterations;
            this.arraySize = arraySize;
        }
        public void run()
        {
            try
            {
                cyclicBarrier.SignalAndWait();

                for (long i = 0; i < iterations; i++)
                {
                    long sequence = ringBuffer.Next();
                    long[] @event = ringBuffer.Get(sequence);
                    for (int j = 0; j < arraySize; j++)
                    {
                        @event[j] = i + j;
                    }
                    ringBuffer.Publish(sequence);
                }
            }
            catch (Exception ex)
            {
                throw ;
            }
        }
    }
}
