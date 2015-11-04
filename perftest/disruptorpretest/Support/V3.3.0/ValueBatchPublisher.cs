using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValueBatchPublisher
    {
        private readonly Barrier cyclicBarrier;
        private readonly RingBuffer<ValueEvent> ringBuffer;
        private readonly long iterations;
        private readonly int batchSize;

        public ValueBatchPublisher(Barrier cyclicBarrier
        , RingBuffer<ValueEvent> ringBuffer
        , long iterations
        , int batchSize)
        {
            this.cyclicBarrier = cyclicBarrier;
            this.ringBuffer = ringBuffer;
            this.iterations = iterations;
            this.batchSize = batchSize;
        }

        public void Run()
        {
            try
            {      
                cyclicBarrier.SignalAndWait();
                for (long i = 0; i < iterations; i += batchSize)
                {
                    long hi = ringBuffer.Next(batchSize);
                    long lo = hi - (batchSize - 1);
                    for (long l = lo; l <= hi; l++)
                    {
                        ValueEvent @event = ringBuffer.Get(l);
                        @event.Value = (l);
                    }
                    ringBuffer.Publish(lo, hi);
                }
             
                //Console.WriteLine( "尚未到达的参与者 "+cyclicBarrier.ParticipantsRemaining+" 已到达 "+cyclicBarrier.CurrentPhaseNumber);
            }
            catch (Exception ex)
            {
                throw new ApplicationException();
            }
        }
    }
}
