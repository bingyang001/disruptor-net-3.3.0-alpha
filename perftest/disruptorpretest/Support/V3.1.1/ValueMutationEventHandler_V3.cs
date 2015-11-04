using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;
using System.Threading;

namespace disruptorpretest.Support
{
    public class ValueMutationEventHandler_V3 : IEventHandler<ValueEvent>
    {

        private readonly Operation operation;
        private _Volatile.PaddedLong value = new _Volatile.PaddedLong(0);
        private long count;
        private CountdownEvent latch;

        public ValueMutationEventHandler_V3(Operation operation)
        {
            this.operation = operation;
        }

        public long Value
        {
            get { return value.ReadUnfenced(); }
        }
        public void Reset(CountdownEvent latch, long expectedCount)
        {
            value.WriteUnfenced(0L);
            this.latch = latch;
            count = expectedCount;
        }
        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            value.WriteUnfenced(operation.Op(value.ReadUnfenced(), @event.Value));
          
            if (count == sequence)
            {
                latch.Signal();               
            }
        }

        #endregion
    }
}
