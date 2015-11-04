using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace disruptorpretest.Support.V3._3._0
{
    public class EventCountingQueueProcessor
    {
        private volatile bool running;
        private readonly BlockingCollection<long> blockingQueue;
        private readonly _Volatile.PaddedLong[] counters;
        private readonly int index;

        public EventCountingQueueProcessor(BlockingCollection<long> blockingQueue
        , _Volatile.PaddedLong[] counters
        , int index)
        {
            this.blockingQueue = blockingQueue;
            this.counters = counters;
            this.index = index;
        }

        public void halt()
        {
            running = false;
        }
        public void run()
        {
            running = true;
            while (running)
            {
                try
                {
                    blockingQueue.Take();
                    counters[index].WriteUnfenced(counters[index].ReadUnfenced() + 1L);
                }
                catch (Exception ex)
                {
                    break;
                }
            }
        }
    }
}
