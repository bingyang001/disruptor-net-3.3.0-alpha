using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class LongArrayEventHandler : IEventHandler<long[]>
    {
        private readonly System.Threading._Volatile.PaddedLong value = new System.Threading._Volatile.PaddedLong();
        private long count;
        private CountdownEvent latch;

        public long Value { get { return value.ReadAcquireFence(); } }

        public void Reset(CountdownEvent latch, long expectedCount)
        {
            value.WriteFullFence(0L);
            this.latch = latch;
            count = expectedCount;
        }

        #region IEventHandler<long[]> 成员

        public void OnEvent(long[] @event, long sequence, bool endOfBatch)
        {
            for (int i = 0; i < @event.Length; i++)
            {
                value.WriteFullFence(value.ReadFullFence() + @event[i]);
            }

            if (--count == 0)
            {
                latch.Signal();
            }
        }

        #endregion
    }
}
