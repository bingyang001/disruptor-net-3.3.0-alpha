using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class FizzBuzzEventHandler2 : IEventHandler<FizzBuzzEvent>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private /*readonly*/ long _iterations;
        private /*readonly*/ CountdownEvent _ce;
        private _Volatile.PaddedLong _fizzBuzzCounter=new _Volatile.PaddedLong ();

        public long FizzBuzzCounter
        {
            get { return _fizzBuzzCounter.ReadFullFence(); }
        }

        public FizzBuzzEventHandler2(FizzBuzzStep fizzBuzzStep)
        {
            _fizzBuzzStep = fizzBuzzStep;                    
        }

        public void rset(CountdownEvent latch, long expectedCount) 
        {
            _fizzBuzzCounter.WriteCompilerOnlyFence(0L);
            _ce = latch;
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
                        _fizzBuzzCounter.WriteFullFence(_fizzBuzzCounter.ReadFullFence() + 1);
                    }
                    break;
            }
            //System.Console.WriteLine(sequence);
            if (_ce!=null && sequence == _iterations)
            {
                _ce.Signal ();
            }
        }

        #endregion
    }
}
