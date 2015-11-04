using System;
using Disruptor;
using System.Threading;

namespace disruptorpretest.Support
{
    public class ValueAdditionEventHandler_V3 : IEventHandler<ValueEvent>
    {
        private _Volatile.PaddedLong _value;
        private long count;
        private ManualResetEvent latch;
        private long localSequence = -1;

        public long Value { get { return _value.ReadFullFence(); } }

        public void Reset(ManualResetEvent latch, long expectedCount)
        {
            _value.WriteUnfenced(0);
            this.latch = latch;
            this.count = expectedCount;
        }

        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            _value.WriteUnfenced(_value.ReadUnfenced() + @event.Value);

            if (localSequence + 1 == sequence)
            {
                localSequence = sequence;
            }
            else
            {
                Console.WriteLine("Expected: " + (localSequence + 1) + "found: " + sequence);
            }

            if (count == sequence)
            {
                latch.Set();
            }
        }

        #endregion
    }
}
