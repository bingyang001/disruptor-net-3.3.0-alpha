using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValueMutationQueueProcessor
    {
        private volatile bool running;
        private long value;
        private long sequence;
        private CountdownEvent latch;

        private readonly ConcurrentQueue<long> blockingQueue;
        private readonly Operation operation;
        private readonly long count;

        public ValueMutationQueueProcessor(ConcurrentQueue<long> blockingQueue, Operation operation, long count)
        {
            this.blockingQueue = blockingQueue;
            this.operation = operation;
            this.count = count;
        }

        public long getValue()
        {
            return value;
        }

        public void reset(CountdownEvent latch)
        {
            value = 0L;
            sequence = 0L;
            this.latch = latch;
        }

        public void halt()
        {
            running = false;
        }

        public void run()
        {
            running = true;
            while (true)
            {
                try
                {
                    long value;
                    blockingQueue.TryDequeue(out value);
                    this.value = operation.Op(this.value, value);

                    if (sequence++ == count)
                    {
                        latch.Signal();
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
