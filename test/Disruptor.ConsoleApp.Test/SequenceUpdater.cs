using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;
namespace DisruptorGeneralTest
{
    public class SequenceUpdater
    {
        private readonly Barrier barrier = new Barrier(2);
        public readonly Sequence sequence = new Sequence();
        private readonly int sleepTime;
        private IWaitStrategy waitStrategy;

        public SequenceUpdater(int sleepTime, IWaitStrategy waitStrategy)
        {
            this.sleepTime = sleepTime;
            this.waitStrategy = waitStrategy;
        }

        public void run()
        {
            try
            {
                barrier.SignalAndWait();
                if (0 != sleepTime)
                {
                    Thread.Sleep(sleepTime);
                }
                sequence.IncrementAndGet();
                waitStrategy.SignalAllWhenBlocking();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void WaitForStartup()
        {
            barrier.SignalAndWait();
        }
    }
}
