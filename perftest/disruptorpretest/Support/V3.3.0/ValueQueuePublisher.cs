using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValueQueuePublisher
    {
        private readonly Barrier cyclicBarrier;
        private readonly ConcurrentQueue<long> blockingQueue;
        private readonly long iterations;

        public ValueQueuePublisher(Barrier cyclicBarrier, ConcurrentQueue<long> blockingQueue, long iterations)
        {
            this.cyclicBarrier = cyclicBarrier;
            this.blockingQueue = blockingQueue;
            this.iterations = iterations;
        }


        public void run()
        {
            try
            {                   
               // cyclicBarrier.SignalAndWait();           
                for (long i = 0; i < iterations; i++)
                {
                    blockingQueue.Enqueue(i);
                }
              
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
