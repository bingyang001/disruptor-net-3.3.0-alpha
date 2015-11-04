using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValueAdditionQueueProcessor
    {
        private volatile bool running;
        private long value;
        private long sequence;
        private CountdownEvent latch;

        private readonly ConcurrentQueue<long> blockingQueue;
        private readonly long count;

        public ValueAdditionQueueProcessor(ConcurrentQueue<long> blockingQueue, long count)
        {
            this.blockingQueue = blockingQueue;
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
                    long _value;
                    blockingQueue.TryDequeue(out _value);
                    Console.Write(_value+",");
                    //TODO:消费
                    this.value += _value;
                    //处理结束，则发出信号
                    if (sequence++ == count)
                    {
                        latch.Signal(); 
                        break;
                    }
                }
                catch (AggregateException ex)
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
