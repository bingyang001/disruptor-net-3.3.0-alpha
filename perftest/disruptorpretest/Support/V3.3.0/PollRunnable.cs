using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class PollRunnable : IEventProcessor, EventPoller<ValueEvent>.IHandler<ValueEvent>
    {

        private readonly EventPoller<ValueEvent> poller;
        private volatile bool running = true;
        private readonly _Volatile.PaddedLong value = new _Volatile.PaddedLong();
        private CountdownEvent latch;
        private long count;
        public PollRunnable(EventPoller<ValueEvent> poller)
        {
            this.poller = poller;
        }
        #region IHandler<ValueEvent> 成员

        public bool OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            value.WriteCompilerOnlyFence(value.ReadCompilerOnlyFence() + @event.Value);

            if (count == sequence)
            {
                latch.Signal();
            }

            return true;
        }
        public long getValue()
        {
            return value.ReadCompilerOnlyFence();
        }
        public void reset(CountdownEvent latch, long expectedCount)
        {
            value.WriteCompilerOnlyFence(0L);
            this.latch = latch;
            count = expectedCount;
            running = true;
        }

        #endregion

        #region IEventProcessor 成员

        public Sequence Sequence
        {
            get { throw new NotImplementedException () ;}
        }

        public void Halt()
        {
            running = false;
        }

        public void Run()
        {
            try
            {
                while (running)
                {

                    if (EventPoller<ValueEvent>.PollState.PROCESSING != poller.Poll(this))
                    {
                        Thread.Yield();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public bool IsRunning()
        {
           return running ;
        }

        #endregion
    }
}
