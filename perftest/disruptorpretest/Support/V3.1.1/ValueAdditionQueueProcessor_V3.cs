using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace disruptorpretest.Support
{
    public class ValueAdditionQueueProcessor_V3
    {
        private volatile bool running;
        private long value;
        private long sequence;
        private ManualResetEvent latch;

        private BlockingCollection<long> blockingQueue;
        private long count;

        public ValueAdditionQueueProcessor_V3(BlockingCollection<long> blockingQueue, long count)
        {
            this.blockingQueue = blockingQueue;
            this.count = count;
        }

        public long Value
        {
            get { return value; }
        }

        public void Reset(ManualResetEvent latch)
        {
            value = 0L;
            sequence = 0L;
            this.latch = latch;
        }

        public void Halt()
        {
            running = false;
        }

        public void Run()
        {
            running = true;
            while (true)
            {
                try
                {                    
                    this.value += blockingQueue.Take();

                    if (sequence++ == count)
                    {
                        latch.Set();
                    }
                }
                catch (Exception ex)
                {
                    if (!running)
                    {
                        break;
                    }
                }
            }
        }
    }

    public class ValueAdditionQueueProcessor
    {
        private volatile bool running;
        private long value;
        private long sequence;
        private CountdownEvent latch;

        private BlockingCollection<long> blockingQueue;
        private long count;

        public ValueAdditionQueueProcessor(BlockingCollection<long> blockingQueue, long count)
        {
            this.blockingQueue = blockingQueue;
            this.count = count;
        }

        public long Value
        {
            get { return value; }
        }

        public void Reset(CountdownEvent latch)
        {
            value = 0L;
            sequence = 0L;
            this.latch = latch;
        }

        public void Halt()
        {
            running = false;
        }

        public void Run()
        {
            running = true;
            while (true)
            {
                try
                {
                    this.value += blockingQueue.Take();

                    if (sequence++ == count)
                    {
                        latch.Signal() ;
                    }
                }
                catch (Exception ex)
                {
                    if (!running)
                    {
                        break;
                    }
                }
            }
        }
    }

}
