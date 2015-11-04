using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class FizzBuzzEventHandler : IEventHandler<FizzBuzzEvent>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private /*readonly*/ long _iterations;
        private /*readonly*/ ManualResetEvent _mru;
        private _Volatile.PaddedLong _fizzBuzzCounter;

        public long FizzBuzzCounter
        {
            get { return _fizzBuzzCounter.ReadUnfenced(); }
        }

        public FizzBuzzEventHandler(FizzBuzzStep fizzBuzzStep, long iterations, ManualResetEvent mru)
        {
            _fizzBuzzStep = fizzBuzzStep;
            _iterations = iterations;
            _mru = mru;
            _fizzBuzzCounter = new _Volatile.PaddedLong(0);
        }

        public void rset(ManualResetEvent latch,  long expectedCount) 
        {
            _fizzBuzzCounter.WriteCompilerOnlyFence(0L);
            this._mru = latch;
            _iterations = expectedCount;
         }
        #region IEventHandler<FizzBuzzEvent> ≥…‘±

        public void OnEvent(FizzBuzzEvent @event, long sequence, bool endOfBatch)
        {
            switch (_fizzBuzzStep)
            {
                case FizzBuzzStep.Fizz:
                    @event.Fizz = (@event.Value % 3) == 0;
                    break;
                case FizzBuzzStep.Buzz:
                    @event.Buzz = (@event.Value % 5) == 0;
                    break;

                case FizzBuzzStep.FizzBuzz:
                    if (@event.Fizz && @event.Buzz)
                    {
                        _fizzBuzzCounter.WriteUnfenced(_fizzBuzzCounter.ReadUnfenced() + 1);
                    }
                    break;
            }
            //System.Console.WriteLine(sequence);
            if (sequence == _iterations)
            {
                _mru.Set();
            }
        }

        #endregion
    }
}
