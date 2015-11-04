using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class ValueAdditionEventHandler : IEventHandler<ValueEvent>
    {
        private long _iterations;
        private _Volatile.PaddedLong _value=new _Volatile.PaddedLong ();
        private CountdownEvent _mru;
        public ValueAdditionEventHandler() { }
        public ValueAdditionEventHandler(long iterations, CountdownEvent mru)
        {
            _iterations = iterations;
            _mru = mru;
        }

        public long Value
        {
            get { return _value.ReadUnfenced(); }
        }

        public void reset(CountdownEvent latch, long expectedCount)
        {
            _value.WriteUnfenced(0L);
            this._mru = latch;
            _iterations = expectedCount;
            Console.WriteLine("expectedCount {0:###,###,###}", expectedCount);
        }

        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            _value.WriteUnfenced(_value.ReadUnfenced() + @event.Value);

            if (_iterations == sequence)
            {
                _mru.Signal();
            }
        }

        #endregion
    }
}
