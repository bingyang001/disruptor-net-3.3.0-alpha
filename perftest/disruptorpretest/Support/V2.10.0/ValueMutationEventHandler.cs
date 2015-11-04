using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class ValueMutationEventHandler : IEventHandler<ValueEvent>
    {
        private readonly Operation _operation;
        private _Volatile.PaddedLong _value = new _Volatile.PaddedLong(0);
        private readonly long _iterations;
        private readonly CountdownEvent _latch;

        public ValueMutationEventHandler(Operation operation, long iterations, CountdownEvent latch)
        {
            _operation = operation;
            _iterations = iterations;
            _latch = latch;
        }

        public long Value
        {
            get { return _value.ReadUnfenced(); }
        }

       
        #region IEventHandler<ValueEvent> ≥…‘±

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            _value.WriteUnfenced(_operation.Op(_value.ReadUnfenced(), @event.Value));

            if (sequence == _iterations - 1)
            {
                _latch.Signal();
            }
        }

        #endregion
    }
}
