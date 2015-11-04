using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class FizzBuzzEventHandler_V3 : IEventHandler<FizzBuzzEvent>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private ManualResetEvent _mru;
        private _Volatile.PaddedLong fizzBuzzCounter;
        private long count;

        public long FizzBuzzCounter
        {
            get { return fizzBuzzCounter.ReadUnfenced(); }
        }

        public FizzBuzzEventHandler_V3(FizzBuzzStep fizzBuzzStep)
        {
            _fizzBuzzStep = fizzBuzzStep;
            fizzBuzzCounter = new _Volatile.PaddedLong(0);
        }

        public void Reset(ManualResetEvent latch, long expectedCount)
        {
            this.fizzBuzzCounter.WriteUnfenced(0L);
            this._mru = latch;
            this.count = expectedCount;
        }

        #region IEventHandler<FizzBuzzEvent> ≥…‘±

        public void OnEvent(FizzBuzzEvent @event, long sequence, bool endOfBatch)
        {
            switch (_fizzBuzzStep)
            {
                case FizzBuzzStep.Fizz:
                    //if (@event.Value % 3 == 0)
                    @event.Fizz = @event.Value % 3 == 0;
                    break;
                case FizzBuzzStep.Buzz:
                    // if (@event.Value % 5 == 0)
                    @event.Buzz = @event.Value % 5 == 0;
                    break;

                case FizzBuzzStep.FizzBuzz:
                    if (@event.Fizz && @event.Buzz)
                        fizzBuzzCounter.WriteUnfenced(fizzBuzzCounter.ReadUnfenced() + 1);
                    break;
            }
            if (_mru != null && count == sequence)
            {
                _mru.Set();
            }
        }

        #endregion
    }
}
