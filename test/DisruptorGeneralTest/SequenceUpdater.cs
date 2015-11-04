using System;
using System.Threading;
using Disruptor;

namespace DisruptorGeneralTest
{
    public class SequenceUpdater
    {
        private readonly Barrier barrier = new Barrier(2);
        public readonly Sequence sequence = new Sequence();
        private readonly TimeSpan sleepTime;
        private IWaitStrategy waitStrategy;

        public SequenceUpdater(TimeSpan sleepTime, IWaitStrategy waitStrategy)
        {
            this.sleepTime = sleepTime;
            this.waitStrategy = waitStrategy;
        }

        public void run()
        {
            try
            {
                barrier.SignalAndWait();
                if (TimeSpan.MinValue != sleepTime && TimeSpan.MaxValue != sleepTime)
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
