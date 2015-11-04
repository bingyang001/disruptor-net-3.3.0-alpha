using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class ValueAdditionEventHandler : IEventHandler<ValueEvent>
    {
        private readonly long _iterations;
        private _Volatile.PaddedLong _value;
        private readonly ManualResetEvent _mru;

        public ValueAdditionEventHandler(long iterations, ManualResetEvent mru)
        {
            _iterations = iterations;
            _mru = mru;
        }

        public long Value
        {
            get { return _value.ReadUnfenced(); }
        }

       

        #region IEventHandler<ValueEvent> ≥…‘±

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            _value.WriteUnfenced(_value.ReadUnfenced() + @event.Value);

            if (sequence == _iterations - 1)
            {
                _mru.Set();
            }
        }

        #endregion
    }
}


