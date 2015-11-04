using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class ValueMutationEventHandler_v3 : IEventHandler<ValueEvent>
    {
        private readonly Operation _operation;
        private _Volatile.PaddedLong _value = new _Volatile.PaddedLong(0);
        private long _iterations;
        private CountdownEvent _latch;

        public ValueMutationEventHandler_v3(Operation operation)
        {
            _operation = operation;

        }
        public void reset(CountdownEvent latch, long expectedCount)
        {
            _value.WriteUnfenced(0L);
            this._latch = latch;
            _iterations = expectedCount;
        }
        public long Value
        {
            get { return _value.ReadUnfenced(); }
        }


        #region IEventHandler<ValueEvent> ≥…‘±

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            _value.WriteUnfenced(_operation.Op(_value.ReadUnfenced(), @event.Value));

            if (sequence == _iterations )
            {
                _latch.Signal();
            }
        }

        #endregion
    }
}
